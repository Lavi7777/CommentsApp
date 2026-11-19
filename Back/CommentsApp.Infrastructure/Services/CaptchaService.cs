using CommentsApp.Domain.Abstractions;
using CommentsApp.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using SkiaSharp;
using System.Security.Cryptography;

namespace CommentsApp.Infrastructure.Services;

public class CaptchaService : ICaptchaService
{
   
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 5;
    private const int Width = 160;
    private const int Height = 60;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;

    public CaptchaService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public CaptchaChallenge Create()
    {
        var code = GenerateCode();
        var id = Guid.NewGuid().ToString("N");

        _cache.Set(CacheKey(id), code, Lifetime);

        return new CaptchaChallenge(id, RenderPng(code));
    }

    public bool Validate(string captchaId, string code)
    {
        if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var key = CacheKey(captchaId);
        if (!_cache.TryGetValue(key, out string? expected))
        {
            return false;
        }

        
        _cache.Remove(key);

        return string.Equals(expected, code.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string CacheKey(string id) => $"captcha:{id}";

    private static string GenerateCode()
    {
        var chars = new char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }

    private static byte[] RenderPng(string code)
    {
        var random = Random.Shared;

        using var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(245, 245, 245));

        // Линии-помехи
        using var linePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f
        };
        for (var i = 0; i < 8; i++)
        {
            linePaint.Color = RandomColor(random, 120, 200);
            canvas.DrawLine(
                random.Next(Width), random.Next(Height),
                random.Next(Width), random.Next(Height),
                linePaint);
        }

        // Символы с поворотом и небольшим смещением по высоте
        using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
        using var font = new SKFont(typeface, 36);
        using var textPaint = new SKPaint { IsAntialias = true };

        for (var i = 0; i < code.Length; i++)
        {
            textPaint.Color = RandomColor(random, 20, 110);

            var x = 14 + i * 29;
            var y = 43 + random.Next(-5, 6);

            canvas.Save();
            canvas.RotateDegrees(random.Next(-18, 19), x + 10, y - 12);
            canvas.DrawText(code[i].ToString(), x, y, SKTextAlign.Left, font, textPaint);
            canvas.Restore();
        }

        // Точки-помехи
        using var dotPaint = new SKPaint { IsAntialias = true };
        for (var i = 0; i < 80; i++)
        {
            dotPaint.Color = RandomColor(random, 100, 200);
            canvas.DrawCircle(random.Next(Width), random.Next(Height), 1f, dotPaint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    private static SKColor RandomColor(Random random, int min, int max)
    {
        return new SKColor(
            (byte)random.Next(min, max),
            (byte)random.Next(min, max),
            (byte)random.Next(min, max));
    }
}