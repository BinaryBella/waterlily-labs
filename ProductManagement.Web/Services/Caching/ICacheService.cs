namespace ProductManagement.Web.Services.Caching;

/// <summary>
/// Generic read-through cache used by the service layer. Each read method takes a key and a
/// <c>loadData</c> delegate. The delegate is a callback that loads the value on a cache miss,
/// so the cache doesn't need to know where any data comes from.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Returns the cached value for <paramref name="key"/>, or calls <paramref name="loadData"/>
    /// and keeps its result with no expiry, until it is removed.
    /// A <c>null</c> result is returned but not cached.
    /// </summary>
    Task<T> CachedLongAsync<T>(string key, Func<Task<T>> loadData);

    /// <summary>
    /// Returns the cached value for <paramref name="key"/>, or calls <paramref name="loadData"/>
    /// and keeps its result for at most 5 minutes (absolute expiry).
    /// A <c>null</c> result is returned but not cached.
    /// </summary>
    Task<T> CachedAsync<T>(string key, Func<Task<T>> loadData);

    /// <summary>Removes the entry for <paramref name="key"/>, if there is one.</summary>
    void Remove(string key);
}
