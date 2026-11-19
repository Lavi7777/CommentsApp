using System.Linq.Expressions;
using CommentsApp.Api.Dtos;
using CommentsApp.Domain.Abstractions;
using CommentsApp.Domain.Entities;
using CommentsApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommentsApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private const int PageSize = 25;

    private static readonly Expression<Func<Comment, CommentDto>> ToDto = c => new CommentDto
    {
        Id = c.Id,
        ParentId = c.ParentId,
        UserName = c.User.UserName,
        Email = c.User.Email,
        HomePage = c.User.HomePage,
        Text = c.Text,
        CreatedAt = c.CreatedAt
    };

    private readonly AppDbContext _db;
    private readonly ICommentTextSanitizer _sanitizer;
    private readonly ICaptchaService _captcha;

    public CommentsController(
        AppDbContext db,
        ICommentTextSanitizer sanitizer,
        ICaptchaService captcha)
    {
        _db = db;
        _sanitizer = sanitizer;
        _captcha = captcha;
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount()
    {
        var count = await _db.Comments.CountAsync();
        return Ok(new { count });
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] string sortBy = "date",
        [FromQuery] bool desc = true)
    {
        if (page < 1)
        {
            page = 1;
        }

        var topLevel = _db.Comments.AsNoTracking().Where(c => c.ParentId == null);
        var totalCount = await topLevel.CountAsync();

        IOrderedQueryable<Comment> ordered = (sortBy.ToLowerInvariant(), desc) switch
        {
            ("username", true) => topLevel.OrderByDescending(c => c.User.UserName),
            ("username", false) => topLevel.OrderBy(c => c.User.UserName),
            ("email", true) => topLevel.OrderByDescending(c => c.User.Email),
            ("email", false) => topLevel.OrderBy(c => c.User.Email),
            (_, false) => topLevel.OrderBy(c => c.CreatedAt),
            _ => topLevel.OrderByDescending(c => c.CreatedAt)
        };
        ordered = ordered.ThenBy(c => c.Id);

        var items = await ordered
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(ToDto)
            .ToListAsync();

        var byId = items.ToDictionary(c => c.Id);
        var currentIds = items.Select(c => c.Id).ToList();

        while (currentIds.Count > 0)
        {
            var replies = await _db.Comments.AsNoTracking()
                .Where(c => c.ParentId != null && currentIds.Contains(c.ParentId.Value))
                .OrderBy(c => c.CreatedAt)
                .Select(ToDto)
                .ToListAsync();

            foreach (var reply in replies)
            {
                byId[reply.Id] = reply;
                byId[reply.ParentId!.Value].Replies.Add(reply);
            }

            currentIds = replies.Select(r => r.Id).ToList();
        }

        return Ok(new PagedResult<CommentDto>
        {
            Items = items,
            Page = page,
            PageSize = PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommentRequest request)
    {
        var sanitized = _sanitizer.Sanitize(request.Text);
        if (!sanitized.IsValid)
        {
            return BadRequest(new { error = sanitized.Error });
        }

        if (sanitized.Html.Length > 5000)
        {
            return BadRequest(new { error = "Text is too long." });
        }

        if (request.ParentId.HasValue)
        {
            var parentExists = await _db.Comments.AnyAsync(c => c.Id == request.ParentId.Value);
            if (!parentExists)
            {
                return BadRequest(new { error = "Parent comment not found." });
            }
        }

        // CAPTCHA проверяем последней: код одноразовый, и он не должен
        // сгорать из-за других ошибок в форме
        if (!_captcha.Validate(request.CaptchaId, request.CaptchaCode))
        {
            return BadRequest(new { error = "Invalid or expired CAPTCHA." });
        }

        var userName = request.UserName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        if (userAgent.Length > 512)
        {
            userAgent = userAgent[..512];
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserName == userName && u.Email == email);

        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = email,
                HomePage = request.HomePage,
                IpAddress = ip,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Text = sanitized.Html,
            CreatedAt = DateTime.UtcNow,
            User = user,
            ParentId = request.ParentId
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        return Ok(new { id = comment.Id });
    }
}