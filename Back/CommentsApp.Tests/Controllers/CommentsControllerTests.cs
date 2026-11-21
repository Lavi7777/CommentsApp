using System.Net;
using CommentsApp.Api.Controllers;
using CommentsApp.Api.Dtos;
using CommentsApp.Domain.Events;
using CommentsApp.Infrastructure.Data;
using CommentsApp.Infrastructure.Services;
using CommentsApp.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommentsApp.Tests.Controllers;

public class CommentsControllerTests : IDisposable
{
    private readonly AppDbContext _db = TestDb.Create();
    private readonly FakeCaptchaService _captcha = new();
    private readonly FakeEventPublisher _events = new();

    public void Dispose()
    {
        _db.Dispose();
    }

    private CommentsController CreateController()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.1.2.3");
        httpContext.Request.Headers.UserAgent = "xunit-agent";

        return new CommentsController(
            _db,
            new CommentTextSanitizer(),
            _captcha,
            new FakeAttachmentStorage(),
            _events,
            new PassthroughCache())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static CreateCommentRequest ValidRequest(string text = "Hello") => new()
    {
        UserName = "Den123",
        Email = "Den@Example.com",
        Text = text,
        CaptchaId = "id",
        CaptchaCode = "code"
    };

    private async Task<PagedResult<CommentDto>> GetPage(int page, string sortBy = "date", bool desc = true)
    {
        var result = await CreateController().GetList(page, sortBy, desc);
        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<PagedResult<CommentDto>>(ok.Value);
    }

    [Fact]
    public async Task Create_saves_comment_user_and_publishes_event()
    {
        var result = await CreateController().Create(ValidRequest("<strong>Hi</strong>"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var comment = await _db.Comments.Include(c => c.User).SingleAsync();
        Assert.Equal("<strong>Hi</strong>", comment.Text);
        Assert.Equal("Den123", comment.User.UserName);
        Assert.Equal("den@example.com", comment.User.Email);
        Assert.Equal("10.1.2.3", comment.User.IpAddress);
        Assert.Equal("xunit-agent", comment.User.UserAgent);
        var published = Assert.IsType<CommentCreatedEvent>(Assert.Single(_events.Published));
        Assert.Equal(comment.Id, published.CommentId);
        Assert.False(published.HasImageAttachment);
    }

    [Fact]
    public async Task Create_escapes_special_characters_in_plain_text()
    {
        await CreateController().Create(ValidRequest("a < b"), CancellationToken.None);

        var comment = await _db.Comments.SingleAsync();
        Assert.Equal("a &lt; b", comment.Text);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<i>not closed")]
    [InlineData("<strong><i>wrong</strong></i>")]
    public async Task Create_rejects_invalid_markup_and_saves_nothing(string text)
    {
        var result = await CreateController().Create(ValidRequest(text), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_db.Comments);
        Assert.Empty(_events.Published);
    }

    [Fact]
    public async Task Create_rejects_wrong_captcha_and_saves_nothing()
    {
        _captcha.IsValid = false;

        var result = await CreateController().Create(ValidRequest(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_db.Comments);
        Assert.Empty(_db.Users);
        Assert.Empty(_events.Published);
    }

    [Fact]
    public async Task Create_rejects_unknown_parent()
    {
        var request = ValidRequest();
        request.ParentId = Guid.NewGuid();

        var result = await CreateController().Create(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_db.Comments);
    }

    [Fact]
    public async Task Create_saves_reply_with_parent_id()
    {
        var user = TestDb.AddUser(_db, "Anna7", "anna@example.com");
        var parent = TestDb.AddComment(_db, user, "parent", DateTime.UtcNow);
        var request = ValidRequest("reply");
        request.ParentId = parent.Id;

        var result = await CreateController().Create(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var reply = await _db.Comments.SingleAsync(c => c.Text == "reply");
        Assert.Equal(parent.Id, reply.ParentId);
    }

    [Fact]
    public async Task Create_reuses_existing_user_with_same_name_and_email()
    {
        await CreateController().Create(ValidRequest("first"), CancellationToken.None);
        await CreateController().Create(ValidRequest("second"), CancellationToken.None);

        Assert.Single(_db.Users);
        Assert.Equal(2, _db.Comments.Count());
    }

    [Fact]
    public async Task GetList_returns_25_per_page_with_totals()
    {
        var user = TestDb.AddUser(_db, "Den123", "den@example.com");
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 30; i++)
        {
            TestDb.AddComment(_db, user, $"c{i}", start.AddMinutes(i));
        }

        var first = await GetPage(1);
        var second = await GetPage(2);
        var third = await GetPage(3);

        Assert.Equal(25, first.Items.Count);
        Assert.Equal(5, second.Items.Count);
        Assert.Empty(third.Items);
        Assert.Equal(30, first.TotalCount);
        Assert.Equal(2, first.TotalPages);
        Assert.Equal(25, first.PageSize);
    }

    [Fact]
    public async Task GetList_treats_page_below_one_as_first_page()
    {
        var page = await GetPage(0);

        Assert.Equal(1, page.Page);
    }

    [Fact]
    public async Task GetList_default_order_is_newest_first()
    {
        var user = TestDb.AddUser(_db, "Den123", "den@example.com");
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        TestDb.AddComment(_db, user, "old", start);
        TestDb.AddComment(_db, user, "new", start.AddHours(1));

        var page = await GetPage(1);

        Assert.Equal(new[] { "new", "old" }, page.Items.Select(c => c.Text));
    }

    [Fact]
    public async Task GetList_sorts_by_user_name_in_both_directions()
    {
        var now = DateTime.UtcNow;
        TestDb.AddComment(_db, TestDb.AddUser(_db, "Bob", "b@example.com"), "b", now);
        TestDb.AddComment(_db, TestDb.AddUser(_db, "Alice", "a@example.com"), "a", now);
        TestDb.AddComment(_db, TestDb.AddUser(_db, "Carl", "c@example.com"), "c", now);

        var ascending = await GetPage(1, "username", false);
        var descending = await GetPage(1, "username", true);

        Assert.Equal(new[] { "Alice", "Bob", "Carl" }, ascending.Items.Select(c => c.UserName));
        Assert.Equal(new[] { "Carl", "Bob", "Alice" }, descending.Items.Select(c => c.UserName));
    }

    [Fact]
    public async Task GetList_sorts_by_email_in_both_directions()
    {
        var now = DateTime.UtcNow;
        TestDb.AddComment(_db, TestDb.AddUser(_db, "U1", "m@example.com"), "1", now);
        TestDb.AddComment(_db, TestDb.AddUser(_db, "U2", "a@example.com"), "2", now);
        TestDb.AddComment(_db, TestDb.AddUser(_db, "U3", "z@example.com"), "3", now);

        var ascending = await GetPage(1, "email", false);
        var descending = await GetPage(1, "email", true);

        Assert.Equal(new[] { "a@example.com", "m@example.com", "z@example.com" }, ascending.Items.Select(c => c.Email));
        Assert.Equal(new[] { "z@example.com", "m@example.com", "a@example.com" }, descending.Items.Select(c => c.Email));
    }

    [Fact]
    public async Task GetList_unknown_sort_falls_back_to_date()
    {
        var user = TestDb.AddUser(_db, "Den123", "den@example.com");
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        TestDb.AddComment(_db, user, "old", start);
        TestDb.AddComment(_db, user, "new", start.AddHours(1));

        var page = await GetPage(1, "nonsense", true);

        Assert.Equal("new", page.Items[0].Text);
    }

    [Fact]
    public async Task GetList_nests_replies_to_any_depth_and_counts_only_top_level()
    {
        var user = TestDb.AddUser(_db, "Den123", "den@example.com");
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var root = TestDb.AddComment(_db, user, "root", now);
        var reply = TestDb.AddComment(_db, user, "reply", now.AddMinutes(1), root.Id);
        TestDb.AddComment(_db, user, "deep reply", now.AddMinutes(2), reply.Id);

        var page = await GetPage(1);

        Assert.Equal(1, page.TotalCount);
        var top = Assert.Single(page.Items);
        Assert.Equal("root", top.Text);
        var level2 = Assert.Single(top.Replies);
        Assert.Equal("reply", level2.Text);
        var level3 = Assert.Single(level2.Replies);
        Assert.Equal("deep reply", level3.Text);
        Assert.Empty(level3.Replies);
    }
}