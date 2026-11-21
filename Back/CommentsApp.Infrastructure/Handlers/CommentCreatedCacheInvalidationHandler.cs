using CommentsApp.Domain.Abstractions;
using CommentsApp.Domain.Events;

namespace CommentsApp.Infrastructure.Handlers;

public class CommentCreatedCacheInvalidationHandler : IDomainEventHandler<CommentCreatedEvent>
{
    private readonly ICommentsCache _cache;

    public CommentCreatedCacheInvalidationHandler(ICommentsCache cache)
    {
        _cache = cache;
    }

    public Task HandleAsync(CommentCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        _cache.Invalidate();
        return Task.CompletedTask;
    }
}