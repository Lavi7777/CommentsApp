using CommentsApp.Domain.Abstractions;
using CommentsApp.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace CommentsApp.Infrastructure.Handlers;

public class CommentCreatedAttachmentProcessingHandler : IDomainEventHandler<CommentCreatedEvent>
{
    private readonly IBackgroundTaskQueue _queue;

    public CommentCreatedAttachmentProcessingHandler(IBackgroundTaskQueue queue)
    {
        _queue = queue;
    }

    public async Task HandleAsync(CommentCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        if (!domainEvent.HasImageAttachment)
        {
            return;
        }

        var commentId = domainEvent.CommentId;

        await _queue.EnqueueAsync(
            (services, token) => services.GetRequiredService<IAttachmentProcessor>().ProcessAsync(commentId, token),
            cancellationToken);
    }
}