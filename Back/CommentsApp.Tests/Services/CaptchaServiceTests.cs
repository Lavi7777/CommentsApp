using System.Text.RegularExpressions;
using CommentsApp.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace CommentsApp.Tests.Services;

public class CaptchaServiceTests
{
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47 };

    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly CaptchaService _service;

    public CaptchaServiceTests()
    {
        _service = new CaptchaService(_cache);
    }

    private string StoredCode(string id) => _cache.Get<string>($"captcha:{id}")!;

    [Fact]
    public void Create_returns_png_image_and_stores_a_five_character_code()
    {
        var challenge = _service.Create();

        Assert.False(string.IsNullOrEmpty(challenge.Id));
        Assert.Equal(PngSignature, challenge.ImagePng.Take(4).ToArray());
        Assert.Matches(new Regex("^[A-HJ-NP-Z2-9]{5}$"), StoredCode(challenge.Id));
    }

    [Fact]
    public void Create_returns_unique_ids()
    {
        var first = _service.Create();
        var second = _service.Create();

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Validate_accepts_the_correct_code()
    {
        var challenge = _service.Create();

        Assert.True(_service.Validate(challenge.Id, StoredCode(challenge.Id)));
    }

    [Fact]
    public void Validate_ignores_case_and_surrounding_spaces()
    {
        var challenge = _service.Create();
        var code = $"  {StoredCode(challenge.Id).ToLowerInvariant()}  ";

        Assert.True(_service.Validate(challenge.Id, code));
    }

    [Fact]
    public void Validate_code_can_be_used_only_once()
    {
        var challenge = _service.Create();
        var code = StoredCode(challenge.Id);

        Assert.True(_service.Validate(challenge.Id, code));
        Assert.False(_service.Validate(challenge.Id, code));
    }

    [Fact]
    public void Validate_wrong_attempt_burns_the_code()
    {
        var challenge = _service.Create();
        var code = StoredCode(challenge.Id);

        Assert.False(_service.Validate(challenge.Id, "WRONG"));
        Assert.False(_service.Validate(challenge.Id, code));
    }

    [Fact]
    public void Validate_rejects_unknown_id()
    {
        Assert.False(_service.Validate("unknown", "ABCDE"));
    }

    [Theory]
    [InlineData("", "ABCDE")]
    [InlineData("id", "")]
    [InlineData(" ", " ")]
    public void Validate_rejects_empty_input(string id, string code)
    {
        Assert.False(_service.Validate(id, code));
    }
}