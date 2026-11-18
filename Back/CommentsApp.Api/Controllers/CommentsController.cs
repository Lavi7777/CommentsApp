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
}
