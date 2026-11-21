using CommentsApp.Api.Hubs;
using CommentsApp.Domain.Abstractions;
using CommentsApp.Domain.Events;
using Microsoft.AspNetCore.SignalR;

namespace CommentsApp.Api.Handlers;

public class CommentCreatedBroadcastHandler : IDomainEventHandler<CommentCreatedEvent>
{
    private readonly IHubContext<CommentsHub> _hub;

    public CommentCreatedBroadcastHandler(IHubContext<CommentsHub> hub)
    {
        _hub = hub;
    }

    public Task HandleAsync(CommentCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        return _hub.Clients.All.SendAsync(
            CommentsHub.CommentCreated,
            new { id = domainEvent.CommentId, parentId = domainEvent.ParentId },
            cancellationToken);
    }
}