using CommentsApp.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CommentsApp.Infrastructure.Events;

public class EventPublisher : IEventPublisher
{
    private readonly IServiceProvider _services;

    public EventPublisher(IServiceProvider services)
    {
        _services = services;
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var handlers = _services.GetServices<IDomainEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }
}