using CommentsApp.Api.Dtos;
using CommentsApp.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace CommentsApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PreviewController : ControllerBase
{
    private readonly ICommentTextSanitizer _sanitizer;

    public PreviewController(ICommentTextSanitizer sanitizer)
    {
        _sanitizer = sanitizer;
    }

    [HttpPost]
    public IActionResult Post([FromBody] PreviewRequest request)
    {
        var result = _sanitizer.Sanitize(request.Text);
        if (!result.IsValid)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { html = result.Html });
    }
}