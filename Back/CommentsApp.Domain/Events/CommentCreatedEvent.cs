using CommentsApp.Domain.Abstractions;

namespace CommentsApp.Domain.Events;

public record CommentCreatedEvent(Guid CommentId, Guid? ParentId) : IDomainEvent;