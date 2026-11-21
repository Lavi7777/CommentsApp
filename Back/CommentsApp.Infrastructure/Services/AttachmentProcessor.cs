using CommentsApp.Domain.Abstractions;
using CommentsApp.Domain.Entities;
using CommentsApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommentsApp.Infrastructure.Services;

public class AttachmentProcessor : IAttachmentProcessor
{
    private readonly AppDbContext _db;
    private readonly IAttachmentStorage _storage;
    private readonly ICommentsCache _cache;
    private readonly ILogger<AttachmentProcessor> _logger;

    public AttachmentProcessor(
        AppDbContext db,
        IAttachmentStorage storage,
        ICommentsCache cache,
        ILogger<AttachmentProcessor> logger)
    {
        _db = db;
        _storage = storage;
        _cache = cache;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid commentId, CancellationToken cancellationToken)
    {
        var attachment = await _db.Attachments.FirstOrDefaultAsync(
            a => a.CommentId == commentId && a.Type == AttachmentType.Image,
            cancellationToken);

        if (attachment is null)
        {
            return;
        }

        var size = await _storage.OptimizeImageAsync(attachment.StoredFileName, cancellationToken);
        if (size is null)
        {
            _logger.LogWarning("Image {File} could not be optimized.", attachment.StoredFileName);
            return;
        }

        attachment.SizeBytes = size.Value;
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
    }
}