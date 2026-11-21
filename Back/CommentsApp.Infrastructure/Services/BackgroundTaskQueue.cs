using System.Threading.Channels;
using CommentsApp.Domain.Abstractions;

namespace CommentsApp.Infrastructure.Services;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<BackgroundWorkItem> _channel = Channel.CreateBounded<BackgroundWorkItem>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait });

    public ValueTask EnqueueAsync(BackgroundWorkItem workItem, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(workItem, cancellationToken);
    }

    public ValueTask<BackgroundWorkItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}