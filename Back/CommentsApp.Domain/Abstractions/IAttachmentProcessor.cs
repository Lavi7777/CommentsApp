namespace CommentsApp.Domain.Abstractions;

public interface IAttachmentProcessor
{
    Task ProcessAsync(Guid commentId, CancellationToken cancellationToken);
}