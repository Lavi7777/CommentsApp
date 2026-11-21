namespace CommentsApp.Domain.Abstractions;

public interface ICommentsCache
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
        where T : class;

    void Invalidate();
}