using CommentsApp.Api.Dtos;
using CommentsApp.Domain.Entities;
using CommentsApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommentsApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CommentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount()
    {
        var count = await _db.Comments.CountAsync();
        return Ok(new { count });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommentRequest request)
    {
        if (request.ParentId.HasValue)
        {
            var parentExists = await _db.Comments.AnyAsync(c => c.Id == request.ParentId.Value);
            if (!parentExists)
            {
                return BadRequest(new { error = "Parent comment not found." });
            }
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
            Text = request.Text,
            CreatedAt = DateTime.UtcNow,
            User = user,
            ParentId = request.ParentId
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        return Ok(new { id = comment.Id });
    }
}