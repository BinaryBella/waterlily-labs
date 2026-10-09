using Moq;
using ProductManagement.Web.Models.Dtos;
using ProductManagement.Web.Repositories;
using ProductManagement.Web.Services;
using ProductManagement.Web.Services.Caching;

namespace ProductManagement.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ReportService"/>. The calculations themselves run in stored
/// procedures and were checked against the seed data, so these tests cover only what the
/// service adds: the 5-minute cache with the right keys, and passing every row through.
/// </summary>
[TestClass]
public sealed class ReportServiceTests
{
    private Mock<IProductRepository> _repository = null!;
    private Mock<ICacheService> _cache = null!;
    private ReportService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new Mock<IProductRepository>();
        _cache = new Mock<ICacheService>();

        // The cache mock runs the loader delegate, as on a cache miss.
        _cache.Setup(c => c.CachedAsync(It.IsAny<string>(), It.IsAny<Func<Task<IReadOnlyList<CategoryAveragePrice>>>>()))
            .Returns((string _, Func<Task<IReadOnlyList<CategoryAveragePrice>>> loadData) => loadData());
        _cache.Setup(c => c.CachedAsync(It.IsAny<string>(), It.IsAny<Func<Task<IReadOnlyList<CategoryStockValue>>>>()))
            .Returns((string _, Func<Task<IReadOnlyList<CategoryStockValue>>> loadData) => loadData());

        _service = new ReportService(_repository.Object, _cache.Object);
    }

    [TestMethod]
    public async Task GetAveragePriceByCategoryAsync_Called_ReturnsRowsThroughFiveMinuteCache()
    {
        // Arrange
        var rows = new List<CategoryAveragePrice>
        {
            new("Electronics", 40.00m, 3),
            new("Furniture", 275.00m, 2),
        };
        _repository.Setup(r => r.GetAveragePriceByCategoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(rows);

        // Act
        var result = await _service.GetAveragePriceByCategoryAsync();

        // Assert
        CollectionAssert.AreEqual(rows, result.ToList());
        _cache.Verify(c => c.CachedAsync(CacheKeys.AveragePriceByCategory, It.IsAny<Func<Task<IReadOnlyList<CategoryAveragePrice>>>>()), Times.Once);
        _cache.Verify(c => c.CachedLongAsync(It.IsAny<string>(), It.IsAny<Func<Task<IReadOnlyList<CategoryAveragePrice>>>>()), Times.Never);
    }

    [TestMethod]
    public async Task GetHighestStockValueCategoryAsync_Tie_ReturnsEveryTopCategory()
    {
        // Arrange: the procedure returns one row per category that shares the top value.
        var rows = new List<CategoryStockValue>
        {
            new("Electronics", 2400.00m),
            new("Furniture", 2400.00m),
        };
        _repository.Setup(r => r.GetHighestStockValueCategoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(rows);

        // Act
        var result = await _service.GetHighestStockValueCategoryAsync();

        // Assert
        CollectionAssert.AreEqual(rows, result.ToList());
        _cache.Verify(c => c.CachedAsync(CacheKeys.HighestStockValueCategory, It.IsAny<Func<Task<IReadOnlyList<CategoryStockValue>>>>()), Times.Once);
    }

    [TestMethod]
    public async Task GetHighestStockValueCategoryAsync_NoProducts_ReturnsEmptyList()
    {
        // Arrange
        _repository.Setup(r => r.GetHighestStockValueCategoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryStockValue>());

        // Act
        var result = await _service.GetHighestStockValueCategoryAsync();

        // Assert
        Assert.IsEmpty(result);
    }
}
