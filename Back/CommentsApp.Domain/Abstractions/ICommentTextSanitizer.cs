using CommentsApp.Domain.Models;

namespace CommentsApp.Domain.Abstractions;

public interface ICommentTextSanitizer
{
    SanitizeResult Sanitize(string input);
}