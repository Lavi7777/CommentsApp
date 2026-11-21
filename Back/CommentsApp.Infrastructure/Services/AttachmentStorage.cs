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

    public async Task<long?> OptimizeImageAsync(
        string storedFileName,
        CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_rootPath, Path.GetFileName(storedFileName));
        if (!File.Exists(path))
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);

        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        if (codec is null)
        {
            return null;
        }

        var isJpeg = codec.EncodedFormat == SKEncodedImageFormat.Jpeg;

        using var bitmap = SKBitmap.Decode(data);
        if (bitmap is null)
        {
            return null;
        }

        var scale = Math.Min(
            (double)MaxImageWidth / bitmap.Width,
            (double)MaxImageHeight / bitmap.Height);

        var resized = bitmap;
        var needsDispose = false;

        if (scale < 1.0)
        {
            var width = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
            var height = Math.Max(1, (int)Math.Round(bitmap.Height * scale));

            var scaled = bitmap.Resize(
                new SKImageInfo(width, height),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

            if (scaled is null)
            {
                return null;
            }

            resized = scaled;
            needsDispose = true;
        }

        try
        {
            var format = isJpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png;

            using var image = SKImage.FromBitmap(resized);
            using var encoded = image.Encode(format, 85);
            var outBytes = encoded.ToArray();

            var tempPath = path + ".tmp";
            await File.WriteAllBytesAsync(tempPath, outBytes, cancellationToken);
            File.Move(tempPath, path, overwrite: true);

            return outBytes.Length;
        }
        finally
        {
            if (needsDispose)
            {
                resized.Dispose();
            }
        }
    }

    private async Task<AttachmentSaveResult> SaveTextAsync(
        byte[] bytes, string originalName, CancellationToken cancellationToken)
    {
        if (bytes.Contains((byte)0))
        {
            return Fail("File is not a valid text file.");
        }

        var storedName = $"{Guid.NewGuid():N}.txt";
        await File.WriteAllBytesAsync(Path.Combine(_rootPath, storedName), bytes, cancellationToken);

        return Ok(new StoredAttachment(AttachmentType.Text, originalName, storedName, bytes.Length));
    }

    private async Task<AttachmentSaveResult> SaveImageAsync(
        byte[] bytes, string originalName, CancellationToken cancellationToken)
    {
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);

        if (codec is null)
        {
            return Fail("File is not a valid JPG, GIF or PNG image.");
        }

        var extension = codec.EncodedFormat switch
        {
            SKEncodedImageFormat.Jpeg => ".jpg",
            SKEncodedImageFormat.Png => ".png",
            SKEncodedImageFormat.Gif => ".png",
            _ => null
        };

        if (extension is null)
        {
            return Fail("File is not a valid JPG, GIF or PNG image.");
        }

        var storedName = $"{Guid.NewGuid():N}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(_rootPath, storedName), bytes, cancellationToken);

        return Ok(new StoredAttachment(AttachmentType.Image, originalName, storedName, bytes.Length));
    }

    private static async Task<byte[]?> ReadLimitedAsync(Stream stream, long limit, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;

        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
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