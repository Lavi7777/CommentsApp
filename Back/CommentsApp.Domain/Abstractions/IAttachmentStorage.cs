using CommentsApp.Domain.Models;

namespace CommentsApp.Domain.Abstractions;

public interface IAttachmentStorage
{
    Task<AttachmentSaveResult> SaveAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken = default);
}