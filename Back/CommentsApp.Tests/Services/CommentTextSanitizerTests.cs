using CommentsApp.Infrastructure.Services;

namespace CommentsApp.Tests.Services;

public class CommentTextSanitizerTests
{
    private readonly CommentTextSanitizer _sanitizer = new();

    [Theory]
    [InlineData("plain text", "plain text")]
    [InlineData("a < b & c", "a &lt; b &amp; c")]
    [InlineData("<strong>bold</strong>", "<strong>bold</strong>")]
    [InlineData("<i>it</i> and <code>code</code>", "<i>it</i> and <code>code</code>")]
    [InlineData("<STRONG>Up</STRONG>", "<strong>Up</strong>")]
    [InlineData("<strong><i>nested</i></strong>", "<strong><i>nested</i></strong>")]
    [InlineData(
        "<a href=\"https://example.com\" title=\"site\">link</a>",
        "<a href=\"https://example.com\" title=\"site\">link</a>")]
    [InlineData(
        "<a href=\"http://example.com\">link</a>",
        "<a href=\"http://example.com\">link</a>")]
    public void Valid_text_is_accepted_and_normalized(string input, string expected)
    {
        var result = _sanitizer.Sanitize(input);

        Assert.True(result.IsValid);
        Assert.Null(result.Error);
        Assert.Equal(expected, result.Html);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=\"x\" onerror=\"alert(1)\">")]
    [InlineData("<b>bold</b>")]
    [InlineData("<i>not closed")]
    [InlineData("closed but never opened</strong>")]
    [InlineData("<strong><i>wrong order</strong></i>")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>")]
    [InlineData("<a href=\"https://example.com\" onclick=\"x()\">x</a>")]
    [InlineData("<a href=\"/relative\">x</a>")]
    public void Invalid_text_is_rejected(string input)
    {
        var result = _sanitizer.Sanitize(input);

        Assert.False(result.IsValid);
        Assert.False(string.IsNullOrEmpty(result.Error));
        Assert.Equal(string.Empty, result.Html);
    }

    [Fact]
    public void Unclosed_tag_error_names_the_tag()
    {
        var result = _sanitizer.Sanitize("<i>text");

        Assert.Contains("<i>", result.Error);
        Assert.Contains("not closed", result.Error);
    }

    [Fact]
    public void Special_characters_in_link_attributes_are_encoded()
    {
        var result = _sanitizer.Sanitize("<a href=\"https://example.com/?a=1&b=2\">x</a>");

        Assert.True(result.IsValid);
        Assert.Equal("<a href=\"https://example.com/?a=1&amp;b=2\">x</a>", result.Html);
    }

    [Fact]
    public void Empty_text_is_valid_and_stays_empty()
    {
        var result = _sanitizer.Sanitize(string.Empty);

        Assert.True(result.IsValid);
        Assert.Equal(string.Empty, result.Html);
    }
}