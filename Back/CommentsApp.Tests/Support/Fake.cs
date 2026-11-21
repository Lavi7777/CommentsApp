using CommentsApp.Domain.Abstractions;
using CommentsApp.Domain.Models;

namespace CommentsApp.Tests.Support;

public sealed class FakeCaptchaService : ICaptchaService
{
    public bool IsValid { get; set; } = true;

    public CaptchaChallenge Create() => throw new NotSupportedException();

    public bool Validate(string captchaId, string code) => IsValid;
}

public sealed class FakeEventPublisher : IEventPublisher
{
    public List<IDomainEvent> Published { get; } = new();

    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        Published.Add(domainEvent);
        return Task.CompletedTask;
    }
}

public sealed class PassthroughCache : ICommentsCache
{
    public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
        where T : class
    {
        return factory();
    }

    public void Invalidate()
    {
    }
}

public sealed class FakeAttachmentStorage : IAttachmentStorage
{
    public Task<AttachmentSaveResult> SaveAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AttachmentSaveResult(false, null, "Not supported in tests."));
    }

    public Task<long?> OptimizeImageAsync(
        string storedFileName,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<long?>(null);
    }
}