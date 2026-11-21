using CommentsApp.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace CommentsApp.Tests.Services;

public class CommentsCacheTests
{
    private readonly CommentsCache _cache = new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task Same_key_is_loaded_only_once()
    {
        var calls = 0;

        Task<string> Factory()
        {
            calls++;
            return Task.FromResult("value");
        }

        var first = await _cache.GetOrCreateAsync("k", Factory);
        var second = await _cache.GetOrCreateAsync("k", Factory);

        Assert.Equal(1, calls);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task Different_keys_are_cached_separately()
    {
        var calls = 0;

        Task<string> Factory()
        {
            calls++;
            return Task.FromResult("value");
        }

        await _cache.GetOrCreateAsync("a", Factory);
        await _cache.GetOrCreateAsync("b", Factory);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Invalidate_forces_a_reload()
    {
        var calls = 0;

        Task<string> Factory()
        {
            calls++;
            return Task.FromResult("value");
        }

        await _cache.GetOrCreateAsync("k", Factory);
        _cache.Invalidate();
        await _cache.GetOrCreateAsync("k", Factory);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Value_loaded_during_invalidation_is_not_kept()
    {
        var calls = 0;

        Task<string> Factory()
        {
            calls++;
            _cache.Invalidate();
            return Task.FromResult("stale");
        }

        await _cache.GetOrCreateAsync("k", Factory);
        await _cache.GetOrCreateAsync("k", Factory);

        Assert.Equal(2, calls);
    }
}