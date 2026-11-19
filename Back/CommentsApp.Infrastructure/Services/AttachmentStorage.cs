using CommentsApp.Domain.Abstractions;
using CommentsApp.Domain.Entities;
using CommentsApp.Domain.Models;
using SkiaSharp;

namespace CommentsApp.Infrastructure.Services;

public class AttachmentStorage : IAttachmentStorage
{
    private const int MaxImageWidth = 320;
    private const int MaxImageHeight = 240;
    private const long MaxTextBytes = 100 * 1024;
    private const long MaxImageUploadBytes = 5 * 1024 * 1024;

    private readonly string _rootPath;

    public AttachmentStorage(string rootPath)
    {
        _rootPath = rootPath;
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<AttachmentSaveResult> SaveAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        var safeName = Path.GetFileName(originalFileName);
        if (safeName.Length > 255)
        {
            safeName = safeName[^255..];
        }

        var extension = Path.GetExtension(safeName).ToLowerInvariant();

       
        var limit = extension == ".txt" ? MaxTextBytes : MaxImageUploadBytes;
        var bytes = await ReadLimitedAsync(content, limit, cancellationToken);
        if (bytes is null)
        {
            return Fail(extension == ".txt"
                ? "Text file must not be larger than 100 KB."
                : "Image file is too large.");
        }

        if (extension == ".txt")
        {
            return await SaveTextAsync(bytes, safeName, cancellationToken);
        }

        if (extension is ".jpg" or ".jpeg" or ".png" or ".gif")
        {
            return await SaveImageAsync(bytes, safeName, cancellationToken);
        }

        return Fail("Allowed files: JPG, GIF, PNG images and TXT text files.");
    }

    private async Task<AttachmentSaveResult> SaveTextAsync(
        byte[] bytes, string originalName, CancellationToken ct)
    {
       
        if (bytes.Contains((byte)0))
        {
            return Fail("File is not a valid text file.");
        }

        var storedName = $"{Guid.NewGuid():N}.txt";
        await File.WriteAllBytesAsync(Path.Combine(_rootPath, storedName), bytes, ct);

        return Ok(new StoredAttachment(AttachmentType.Text, originalName, storedName, bytes.Length));
    }

    private async Task<AttachmentSaveResult> SaveImageAsync(
        byte[] bytes, string originalName, CancellationToken ct)
    {
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);

        if (codec is null ||
            codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg
                or SKEncodedImageFormat.Png
                or SKEncodedImageFormat.Gif))
        {
            return Fail("File is not a valid JPG, GIF or PNG image.");
        }

        var sourceFormat = codec.EncodedFormat;

        using var bitmap = SKBitmap.Decode(data);
        if (bitmap is null)
        {
            return Fail("Image could not be read.");
        }

        var scale = Math.Min(
            (double)MaxImageWidth / bitmap.Width,
            (double)MaxImageHeight / bitmap.Height);

        SKBitmap resized = bitmap;
        var needsDispose = false;

        if (scale < 1.0)
        {
            var width = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
            var height = Math.Max(1, (int)Math.Round(bitmap.Height * scale));

            resized = bitmap.Resize(
                new SKImageInfo(width, height),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

            if (resized is null)
            {
                return Fail("Image could not be resized.");
            }

            needsDispose = true;
        }

        try
        {
          
            var isJpeg = sourceFormat == SKEncodedImageFormat.Jpeg;
            var outFormat = isJpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png;
            var outExtension = isJpeg ? ".jpg" : ".png";

            using var image = SKImage.FromBitmap(resized);
            using var encoded = image.Encode(outFormat, 85);
            var outBytes = encoded.ToArray();

            var storedName = $"{Guid.NewGuid():N}{outExtension}";
            await File.WriteAllBytesAsync(Path.Combine(_rootPath, storedName), outBytes, ct);

            return Ok(new StoredAttachment(AttachmentType.Image, originalName, storedName, outBytes.Length));
        }
        finally
        {
            if (needsDispose)
            {
                resized.Dispose();
            }
        }
    }

    private static async Task<byte[]?> ReadLimitedAsync(Stream stream, long limit, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;

        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > limit)
            {
                return null;
            }
        }

        return buffer.ToArray();
    }

    private static AttachmentSaveResult Ok(StoredAttachment attachment) => new(true, attachment, null);

    private static AttachmentSaveResult Fail(string error) => new(false, null, error);
}