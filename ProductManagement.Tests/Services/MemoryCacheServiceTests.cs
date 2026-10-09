using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using ProductManagement.Web.Services.Caching;

namespace ProductManagement.Tests.Services;

/// <summary>
/// Tests for <see cref="MemoryCacheService"/> against a real <see cref="MemoryCache"/>.
/// A fake clock lets the tests move time forward to check expiry without waiting.
/// </summary>
[TestClass]
public sealed class MemoryCacheServiceTests
{
    private const string Key = "test-key";

    private FakeClock _clock = null!;
    private MemoryCache _memoryCache = null!;
    private MemoryCacheService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _clock = new FakeClock(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        _memoryCache = new MemoryCache(new MemoryCacheOptions { Clock = _clock });
        _service = new MemoryCacheService(_memoryCache);
    }

    [TestCleanup]
    public void Cleanup() => _memoryCache.Dispose();

    [TestMethod]
    public async Task CachedAsync_RepeatedCalls_LoaderRunsOnce()
    {
        // Arrange
        var loader = new CountingLoader<string>("value");

        // Act
        var first = await _service.CachedAsync(Key, loader.Load);
        var second = await _service.CachedAsync(Key, loader.Load);
        var third = await _service.CachedAsync(Key, loader.Load);

        // Assert
        Assert.AreEqual(1, loader.Calls);
        Assert.AreEqual("value", first);
        Assert.AreEqual("value", second);
        Assert.AreEqual("value", third);
    }

    [TestMethod]
    public async Task CachedLongAsync_RepeatedCalls_LoaderRunsOnce()
    {
        // Arrange
        var loader = new CountingLoader<string>("value");

        // Act
        await _service.CachedLongAsync(Key, loader.Load);
        await _service.CachedLongAsync(Key, loader.Load);
        var result = await _service.CachedLongAsync(Key, loader.Load);

        // Assert
        Assert.AreEqual(1, loader.Calls);
        Assert.AreEqual("value", result);
    }

    [TestMethod]
    public async Task CachedAsync_JustBeforeFiveMinutes_ReturnsCachedValue()
    {
        // Arrange
        var loader = new CountingLoader<string>("value");
        await _service.CachedAsync(Key, loader.Load);

        // Act
        _clock.Advance(TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds(1));
        await _service.CachedAsync(Key, loader.Load);

        // Assert
        Assert.AreEqual(1, loader.Calls);
    }

    [TestMethod]
    public async Task CachedAsync_AfterFiveMinutes_ReloadsValue()
    {
        // Arrange
        var loader = new CountingLoader<string>("value");
        await _service.CachedAsync(Key, loader.Load);

        // Act
        _clock.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));
        await _service.CachedAsync(Key, loader.Load);

        // Assert
        Assert.AreEqual(2, loader.Calls);
    }

    [TestMethod]
    public async Task CachedAsync_ReadDuringLifetime_DoesNotExtendExpiry()
    {
        // Arrange: a read at 4 minutes would push expiry out if it were sliding, not absolute.
        var loader = new CountingLoader<string>("value");
        await _service.CachedAsync(Key, loader.Load);
        _clock.Advance(TimeSpan.FromMinutes(4));
        await _service.CachedAsync(Key, loader.Load);

        // Act
        _clock.Advance(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1));
        await _service.CachedAsync(Key, loader.Load);

        // Assert
        Assert.AreEqual(2, loader.Calls);
    }

    [TestMethod]
    public async Task CachedLongAsync_AfterThirtyDays_ReturnsCachedValue()
    {
        // Arrange
        var loader = new CountingLoader<string>("value");
        await _service.CachedLongAsync(Key, loader.Load);

        // Act
        _clock.Advance(TimeSpan.FromDays(30));
        await _service.CachedLongAsync(Key, loader.Load);

        // Assert
        Assert.AreEqual(1, loader.Calls);
    }

    [TestMethod]
    public async Task CachedAsync_LoaderReturnsNull_ResultNotCached()
    {
        // Arrange
        var loader = new CountingLoader<string?>(null);

        // Act
        var first = await _service.CachedAsync(Key, loader.Load);
        var second = await _service.CachedAsync(Key, loader.Load);

        // Assert
        Assert.IsNull(first);
        Assert.IsNull(second);
        Assert.AreEqual(2, loader.Calls);
        Assert.IsFalse(_memoryCache.TryGetValue(Key, out _));
    }

    [TestMethod]
    public async Task CachedLongAsync_LoaderReturnsNull_ResultNotCached()
    {
        // Arrange
        var loader = new CountingLoader<string?>(null);

        // Act
        await _service.CachedLongAsync(Key, loader.Load);
        await _service.CachedLongAsync(Key, loader.Load);

        // Assert
        Assert.AreEqual(2, loader.Calls);
        Assert.IsFalse(_memoryCache.TryGetValue(Key, out _));
    }

    [TestMethod]
    public async Task Remove_CachedKey_NextCallReloads()
    {
        // Arrange
        var loader = new CountingLoader<string>("value");
        await _service.CachedLongAsync(Key, loader.Load);

        // Act
        _service.Remove(Key);
        await _service.CachedLongAsync(Key, loader.Load);

        // Assert
        Assert.AreEqual(2, loader.Calls);
    }

    [TestMethod]
    public async Task CachedAsync_DifferentKeys_EachLoadsOnce()
    {
        // Arrange
        var loaderA = new CountingLoader<string>("a");
        var loaderB = new CountingLoader<string>("b");

        // Act
        var a = await _service.CachedAsync("key-a", loaderA.Load);
        var b = await _service.CachedAsync("key-b", loaderB.Load);
        await _service.CachedAsync("key-a", loaderA.Load);

        // Assert
        Assert.AreEqual("a", a);
        Assert.AreEqual("b", b);
        Assert.AreEqual(1, loaderA.Calls);
        Assert.AreEqual(1, loaderB.Calls);
    }

    /// <summary>A loader delegate target that counts how many times the cache called it.</summary>
    private sealed class CountingLoader<T>(T value)
    {
        public int Calls { get; private set; }

        public Task<T> Load()
        {
            Calls++;
            return Task.FromResult(value);
        }
    }

    /// <summary>Clock the MemoryCache reads its time from, moved forward by the tests.</summary>
    private sealed class FakeClock(DateTimeOffset start) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; private set; } = start;

        public void Advance(TimeSpan by) => UtcNow += by;
    }
}
