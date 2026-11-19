using System;
using System.Collections.Generic;
using System.Text;

namespace CommentsApp.Domain.Models
{
    public record CaptchaChallenge(string Id, byte[] ImagePng);
}
