using CommentsApp.Domain.Models;

namespace CommentsApp.Domain.Abstractions;

public interface ICaptchaService
{
    CaptchaChallenge Create();

    bool Validate(string captchaId, string code);
}