using CommentsApp.Domain.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace CommentsApp.Infrastructure.Services;

public class CommentsCache : ICommentsCache
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;
    private CancellationTokenSource _invalidation = new();

    public CommentsCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
        where T : class
    {
        var cacheKey = $"comments:{key}";

        if (_cache.TryGetValue(cacheKey, out T? cached) && cached is not null)
        {
            return cached;
        }

        var token = Volatile.Read(ref _invalidation).Token;
        var value = await factory();

        using var entry = _cache.CreateEntry(cacheKey);
        entry.Value = value;
        entry.AbsoluteExpirationRelativeToNow = Lifetime;
        entry.AddExpirationToken(new CancellationChangeToken(token));

        return value;
    }

    public void Invalidate()
    {
        var previous = Interlocked.Exchange(ref _invalidation, new CancellationTokenSource());
        previous.Cancel();
    }
}