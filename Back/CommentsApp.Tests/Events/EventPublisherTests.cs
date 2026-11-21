using CommentsApp.Domain.Abstractions;
using CommentsApp.Domain.Events;
using CommentsApp.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace CommentsApp.Tests.Events;

public class EventPublisherTests
{
    private sealed class RecordingHandler : IDomainEventHandler<CommentCreatedEvent>
    {
        private readonly string _name;
        private readonly List<string> _calls;

        public RecordingHandler(string name, List<string> calls)
        {
            _name = name;
            _calls = calls;
        }

        public Task HandleAsync(CommentCreatedEvent domainEvent, CancellationToken cancellationToken)
        {
            _calls.Add(_name);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingHandler : IDomainEventHandler<CommentCreatedEvent>
    {
        public Task HandleAsync(CommentCreatedEvent domainEvent, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("boom");
        }
    }

    [Fact]
    public async Task All_handlers_are_called_in_registration_order()
    {
        var calls = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventHandler<CommentCreatedEvent>>(new RecordingHandler("first", calls));
        services.AddSingleton<IDomainEventHandler<CommentCreatedEvent>>(new RecordingHandler("second", calls));
        var publisher = new EventPublisher(services.BuildServiceProvider());

        await publisher.PublishAsync(new CommentCreatedEvent(Guid.NewGuid(), null));

        Assert.Equal(new[] { "first", "second" }, calls);
    }

    [Fact]
    public async Task Publishing_without_handlers_does_nothing()
    {
        var publisher = new EventPublisher(new ServiceCollection().BuildServiceProvider());

        await publisher.PublishAsync(new CommentCreatedEvent(Guid.NewGuid(), null));
    }

    [Fact]
    public async Task Handler_failure_is_propagated()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventHandler<CommentCreatedEvent>>(new FailingHandler());
        var publisher = new EventPublisher(services.BuildServiceProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(new CommentCreatedEvent(Guid.NewGuid(), null)));
    }
}