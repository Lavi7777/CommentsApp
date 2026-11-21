using CommentsApp.Domain.Entities;
using CommentsApp.Domain.Models;
using CommentsApp.Infrastructure.Services;
using SkiaSharp;

namespace CommentsApp.Tests.Services;

public class AttachmentStorageTests : IDisposable
{
    private const string OnePixelGif = "R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7";
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47 };

    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "comments-tests-" + Guid.NewGuid().ToString("N"));

    private readonly AttachmentStorage _storage;

    public AttachmentStorageTests()
    {
        _storage = new AttachmentStorage(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    private Task<AttachmentSaveResult> SaveAsync(byte[] bytes, string fileName)
    {
        return _storage.SaveAsync(new MemoryStream(bytes), fileName);
    }

    private string PathOf(AttachmentSaveResult result)
    {
        return Path.Combine(_root, result.Attachment!.StoredFileName);
    }

    private static byte[] CreateImage(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.OrangeRed);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }

    private static (int Width, int Height) ReadSize(string path)
    {
        using var bitmap = SKBitmap.Decode(path);
        return (bitmap.Width, bitmap.Height);
    }

    [Fact]
    public async Task Text_file_is_saved_with_txt_extension()
    {
        var result = await SaveAsync("hello"u8.ToArray(), "notes.txt");

        Assert.True(result.IsValid);
        Assert.Equal(AttachmentType.Text, result.Attachment!.Type);
        Assert.Equal("notes.txt", result.Attachment.OriginalFileName);
        Assert.EndsWith(".txt", result.Attachment.StoredFileName);
        Assert.Equal(5, result.Attachment.SizeBytes);
        Assert.True(File.Exists(PathOf(result)));
    }

    [Fact]
    public async Task Text_file_of_exactly_100_KB_is_accepted()
    {
        var bytes = Enumerable.Repeat((byte)'a', 100 * 1024).ToArray();

        var result = await SaveAsync(bytes, "limit.txt");

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Text_file_larger_than_100_KB_is_rejected()
    {
        var bytes = Enumerable.Repeat((byte)'a', 100 * 1024 + 1).ToArray();

        var result = await SaveAsync(bytes, "big.txt");

        Assert.False(result.IsValid);
        Assert.Contains("100 KB", result.Error);
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public async Task Text_file_with_binary_content_is_rejected()
    {
        var result = await SaveAsync(new byte[] { 65, 0, 66 }, "binary.txt");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Unsupported_extension_is_rejected()
    {
        var result = await SaveAsync("MZ"u8.ToArray(), "malware.exe");

        Assert.False(result.IsValid);
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public async Task Text_disguised_as_image_is_rejected()
    {
        var result = await SaveAsync("not an image"u8.ToArray(), "fake.png");

        Assert.False(result.IsValid);
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public async Task Image_over_the_upload_limit_is_rejected()
    {
        var result = await SaveAsync(new byte[5 * 1024 * 1024 + 1], "huge.png");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Directory_parts_of_the_file_name_are_ignored()
    {
        var result = await SaveAsync("hello"u8.ToArray(), "../../evil.txt");

        Assert.True(result.IsValid);
        Assert.Equal("evil.txt", result.Attachment!.OriginalFileName);
        Assert.DoesNotContain('/', result.Attachment.StoredFileName);
        Assert.DoesNotContain('\\', result.Attachment.StoredFileName);
    }

    [Theory]
    [InlineData(800, 600, 320, 240)]
    [InlineData(1000, 500, 320, 160)]
    [InlineData(300, 600, 120, 240)]
    public async Task Large_image_is_resized_proportionally(
        int width, int height, int expectedWidth, int expectedHeight)
    {
        var saved = await SaveAsync(CreateImage(width, height, SKEncodedImageFormat.Png), "big.png");
        Assert.True(saved.IsValid);

        var size = await _storage.OptimizeImageAsync(saved.Attachment!.StoredFileName);

        Assert.NotNull(size);
        Assert.Equal((expectedWidth, expectedHeight), ReadSize(PathOf(saved)));
        Assert.Equal(new FileInfo(PathOf(saved)).Length, size!.Value);
    }

    [Fact]
    public async Task Small_image_is_not_enlarged()
    {
        var saved = await SaveAsync(CreateImage(100, 50, SKEncodedImageFormat.Png), "small.png");

        await _storage.OptimizeImageAsync(saved.Attachment!.StoredFileName);

        Assert.Equal((100, 50), ReadSize(PathOf(saved)));
    }

    [Fact]
    public async Task Jpeg_keeps_jpg_extension_and_is_resized()
    {
        var saved = await SaveAsync(CreateImage(400, 400, SKEncodedImageFormat.Jpeg), "photo.jpeg");

        Assert.Equal(AttachmentType.Image, saved.Attachment!.Type);
        Assert.EndsWith(".jpg", saved.Attachment.StoredFileName);

        await _storage.OptimizeImageAsync(saved.Attachment.StoredFileName);

        Assert.Equal((240, 240), ReadSize(PathOf(saved)));
    }

    [Fact]
    public async Task Gif_is_stored_with_png_extension_and_converted_to_png()
    {
        var saved = await SaveAsync(Convert.FromBase64String(OnePixelGif), "anim.gif");

        Assert.True(saved.IsValid);
        Assert.EndsWith(".png", saved.Attachment!.StoredFileName);

        var size = await _storage.OptimizeImageAsync(saved.Attachment.StoredFileName);

        Assert.NotNull(size);
        var header = File.ReadAllBytes(PathOf(saved)).Take(4).ToArray();
        Assert.Equal(PngSignature, header);
    }

    [Fact]
    public async Task Optimize_returns_null_for_missing_file()
    {
        var size = await _storage.OptimizeImageAsync("missing.png");

        Assert.Null(size);
    }

    [Fact]
    public async Task Optimize_leaves_no_temporary_files()
    {
        var saved = await SaveAsync(CreateImage(800, 600, SKEncodedImageFormat.Png), "big.png");

        await _storage.OptimizeImageAsync(saved.Attachment!.StoredFileName);

        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }
}