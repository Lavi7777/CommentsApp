using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using CommentsApp.Domain.Abstractions;
using CommentsApp.Domain.Models;

namespace CommentsApp.Infrastructure.Services;

public class CommentTextSanitizer : ICommentTextSanitizer
{
    private static readonly Regex TagRegex =
        new("<[^<>]*>", RegexOptions.Compiled);

    private static readonly Regex SimpleOpenTag =
        new("^<(i|strong|code)>$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CloseTag =
        new("^</(i|strong|code|a)>$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AnchorOpenTag = new(
        "^<a\\s+href=\"(?<href>[^\"<>]*)\"(?:\\s+title=\"(?<title>[^\"<>]*)\")?\\s*>$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public SanitizeResult Sanitize(string input)
    {
        var result = new StringBuilder();
        var openTags = new Stack<string>();
        var position = 0;

        foreach (Match match in TagRegex.Matches(input))
        {
            result.Append(WebUtility.HtmlEncode(input[position..match.Index]));
            position = match.Index + match.Length;
            var tag = match.Value;

            var simple = SimpleOpenTag.Match(tag);
            if (simple.Success)
            {
                var name = simple.Groups[1].Value.ToLowerInvariant();
                openTags.Push(name);
                result.Append($"<{name}>");
                continue;
            }

            var anchor = AnchorOpenTag.Match(tag);
            if (anchor.Success)
            {
                var href = anchor.Groups["href"].Value;
                var isValidUrl = Uri.TryCreate(href, UriKind.Absolute, out var uri)
                                 && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
                if (!isValidUrl)
                {
                    return Fail("Link must be a valid http or https URL.");
                }

                openTags.Push("a");
                result.Append($"<a href=\"{WebUtility.HtmlEncode(href)}\"");
                if (anchor.Groups["title"].Success)
                {
                    result.Append($" title=\"{WebUtility.HtmlEncode(anchor.Groups["title"].Value)}\"");
                }
                result.Append('>');
                continue;
            }

            var close = CloseTag.Match(tag);
            if (close.Success)
            {
                var name = close.Groups[1].Value.ToLowerInvariant();
                if (openTags.Count == 0 || openTags.Pop() != name)
                {
                    return Fail($"Tag </{name}> is closed in the wrong place.");
                }

                result.Append($"</{name}>");
                continue;
            }

            return Fail($"Tag {tag} is not allowed. Allowed: a, code, i, strong.");
        }

        result.Append(WebUtility.HtmlEncode(input[position..]));

        if (openTags.Count > 0)
        {
            return Fail($"Tag <{openTags.Peek()}> is not closed.");
        }

        return new SanitizeResult(true, result.ToString(), null);
    }

    private static SanitizeResult Fail(string error) => new(false, string.Empty, error);
}