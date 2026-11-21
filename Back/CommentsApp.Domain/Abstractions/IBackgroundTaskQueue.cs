namespace CommentsApp.Domain.Abstractions;

public delegate Task BackgroundWorkItem(IServiceProvider services, CancellationToken cancellationToken);

public interface IBackgroundTaskQueue
{
    ValueTask EnqueueAsync(BackgroundWorkItem workItem, CancellationToken cancellationToken = default);

    ValueTask<BackgroundWorkItem> DequeueAsync(CancellationToken cancellationToken);
}