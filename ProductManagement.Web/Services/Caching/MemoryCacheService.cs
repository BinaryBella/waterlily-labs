using Microsoft.Extensions.Caching.Memory;

namespace ProductManagement.Web.Services.Caching;

/// <summary><see cref="ICacheService"/> backed by the in-process <see cref="IMemoryCache"/>.</summary>
public class MemoryCacheService : ICacheService
{
    /// <summary>Lifetime of entries stored by <see cref="CachedAsync{T}"/>.</summary>
    public static readonly TimeSpan ShortLifetime = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T> CachedLongAsync<T>(string key, Func<Task<T>> loadData)
        => GetOrLoadAsync(key, loadData, expiry: null);

    public Task<T> CachedAsync<T>(string key, Func<Task<T>> loadData)
        => GetOrLoadAsync(key, loadData, ShortLifetime);

    public void Remove(string key) => _cache.Remove(key);

    // Both public methods share this helper and differ only in expiry.
    // Two requests that miss at the same moment may both call loadData. That costs one extra
    // query and is harmless, because they store the same data, so there is no locking.
    private async Task<T> GetOrLoadAsync<T>(string key, Func<Task<T>> loadData, TimeSpan? expiry)
    {
        if (_cache.TryGetValue(key, out T? cached))
        {
            return cached!;
        }

        // The callback runs only on a miss.
        var value = await loadData();

        // "Not found" is never cached, so a product created later is found straight away.
        if (value is not null)
        {
            var options = new MemoryCacheEntryOptions();
            if (expiry is not null)
            {
                // Absolute rather than sliding expiry: the data is never older than the lifetime,
                // however often it is read.
                options.AbsoluteExpirationRelativeToNow = expiry;
            }

            _cache.Set(key, value, options);
        }

        return value;
    }
}
