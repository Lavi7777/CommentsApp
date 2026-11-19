using CommentsApp.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace CommentsApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CaptchaController : ControllerBase
{
    private readonly ICaptchaService _captcha;

    public CaptchaController(ICaptchaService captcha)
    {
        _captcha = captcha;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var challenge = _captcha.Create();

        Response.Headers.CacheControl = "no-store";

        return Ok(new
        {
            id = challenge.Id,
            image = $"data:image/png;base64,{Convert.ToBase64String(challenge.ImagePng)}"
        });
    }
}