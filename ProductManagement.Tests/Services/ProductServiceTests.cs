using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProductManagement.Web.Models.Dtos;
using ProductManagement.Web.Models.Entities;
using ProductManagement.Web.Repositories;
using ProductManagement.Web.Services;
using ProductManagement.Web.Services.Caching;

namespace ProductManagement.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ProductService"/>. The repository and the cache are mocked, so no
/// test touches a database. The cache mock just runs the loader delegate it's given, which keeps
/// the service's own logic under test and lets each test check which cache entries were cleared.
/// </summary>
[TestClass]
public sealed class ProductServiceTests
{
    private Mock<IProductRepository> _repository = null!;
    private Mock<ICacheService> _cache = null!;
    private ProductService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new Mock<IProductRepository>();
        _cache = new Mock<ICacheService>();

        // One pass-through setup per result type the service caches.
        PassThroughCachedLong<IReadOnlyList<ProductDto>>();
        PassThroughCachedLong<IReadOnlyList<string>>();
        PassThroughCached<ProductDto?>();

        _service = new ProductService(_repository.Object, _cache.Object, NullLogger<ProductService>.Instance);
    }

    // ---------- Read all ----------

    [TestMethod]
    public async Task GetAllAsync_ProductsExist_ReturnsProductsAsDtos()
    {
        // Arrange
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>
            {
                NewProduct(1, "Notebook", "Stationery", 2.50m, 200),
                NewProduct(2, "Office Chair", "Furniture", 150.00m, 8),
            });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        CollectionAssert.AreEqual(
            new[]
            {
                new ProductDto(1, "Notebook", "Stationery", 2.50m, 200),
                new ProductDto(2, "Office Chair", "Furniture", 150.00m, 8),
            },
            result.ToList());
    }

    [TestMethod]
    public async Task GetAllAsync_Called_GoesThroughCachedLongWithAllProductsKey()
    {
        // Arrange
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product>());

        // Act
        await _service.GetAllAsync();

        // Assert
        _cache.Verify(c => c.CachedLongAsync(CacheKeys.AllProducts, It.IsAny<Func<Task<IReadOnlyList<ProductDto>>>>()), Times.Once);
        _cache.Verify(c => c.CachedAsync(It.IsAny<string>(), It.IsAny<Func<Task<IReadOnlyList<ProductDto>>>>()), Times.Never);
    }

    [TestMethod]
    public async Task GetAllAsync_NoProducts_ReturnsEmptyList()
    {
        // Arrange
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public async Task GetAllAsync_ProductsExist_ReturnsReadOnlyList()
    {
        // Arrange: a cached list that could be cast back to List<T> could be changed by a caller.
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { NewProduct(1, "Notebook", "Stationery", 2.50m, 200) });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.IsNotInstanceOfType<List<ProductDto>>(result);
        Assert.IsInstanceOfType<System.Collections.ObjectModel.ReadOnlyCollection<ProductDto>>(result);
    }

    // ---------- Read by ID ----------

    [TestMethod]
    public async Task GetByIdAsync_ProductExists_ReturnsDto()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewProduct(7, "Standing Desk", "Furniture", 400.00m, 3));

        // Act
        var result = await _service.GetByIdAsync(7);

        // Assert
        Assert.AreEqual(new ProductDto(7, "Standing Desk", "Furniture", 400.00m, 3), result);
    }

    [TestMethod]
    public async Task GetByIdAsync_ProductMissing_ReturnsNull()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        // Act
        var result = await _service.GetByIdAsync(99);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetByIdAsync_Called_GoesThroughCachedWithProductKey()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewProduct(7, "Standing Desk", "Furniture", 400.00m, 3));

        // Act
        await _service.GetByIdAsync(7);

        // Assert
        _cache.Verify(c => c.CachedAsync(CacheKeys.Product(7), It.IsAny<Func<Task<ProductDto?>>>()), Times.Once);
    }

    // ---------- Categories ----------

    [TestMethod]
    public async Task GetCategoriesAsync_Called_ReturnsRepositoryCategoriesThroughCachedLong()
    {
        // Arrange
        _repository.Setup(r => r.GetCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Electronics", "Furniture" });

        // Act
        var result = await _service.GetCategoriesAsync();

        // Assert
        CollectionAssert.AreEqual(new[] { "Electronics", "Furniture" }, result.ToList());
        _cache.Verify(c => c.CachedLongAsync(CacheKeys.Categories, It.IsAny<Func<Task<IReadOnlyList<string>>>>()), Times.Once);
    }

    // ---------- Create ----------

    [TestMethod]
    public async Task CreateAsync_ValidInput_SavesTrimmedProduct()
    {
        // Arrange
        Product? saved = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback((Product p, CancellationToken _) => saved = p)
            .ReturnsAsync((Product p, CancellationToken _) => WithId(p, 42));

        // Act
        await _service.CreateAsync(new ProductInput("  Desk Lamp ", " Lighting  ", 30.00m, 5));

        // Assert
        Assert.IsNotNull(saved);
        Assert.AreEqual("Desk Lamp", saved.Name);
        Assert.AreEqual("Lighting", saved.Category);
        Assert.AreEqual(30.00m, saved.Price);
        Assert.AreEqual(5, saved.Stock);
    }

    [TestMethod]
    public async Task CreateAsync_ValidInput_ReturnsSuccessWithGeneratedId()
    {
        // Arrange
        _repository.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => WithId(p, 42));

        // Act
        var result = await _service.CreateAsync(new ProductInput("Desk Lamp", "Lighting", 30.00m, 5));

        // Assert
        Assert.AreEqual(ServiceResultStatus.Success, result.Status);
        Assert.AreEqual(new ProductDto(42, "Desk Lamp", "Lighting", 30.00m, 5), result.Value);
    }

    [TestMethod]
    public async Task CreateAsync_ValidInput_ClearsProductAndReportCache()
    {
        // Arrange
        _repository.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => WithId(p, 42));

        // Act
        await _service.CreateAsync(new ProductInput("Desk Lamp", "Lighting", 30.00m, 5));

        // Assert
        VerifyCacheCleared(productId: 42);
    }

    [TestMethod]
    public async Task CreateAsync_ZeroPriceAndStock_IsAccepted()
    {
        // Arrange: zero is the boundary; only negative values are invalid.
        _repository.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => WithId(p, 1));

        // Act
        var result = await _service.CreateAsync(new ProductInput("Free Sample", "Promotions", 0m, 0));

        // Assert
        Assert.AreEqual(ServiceResultStatus.Success, result.Status);
    }

    [TestMethod]
    public async Task CreateAsync_NameAndCategoryAtMaxLength_IsAccepted()
    {
        // Arrange
        _repository.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => WithId(p, 1));
        var input = new ProductInput(new string('n', Product.NameMaxLength), new string('c', Product.CategoryMaxLength), 1m, 1);

        // Act
        var result = await _service.CreateAsync(input);

        // Assert
        Assert.AreEqual(ServiceResultStatus.Success, result.Status);
    }

    // Price is passed as double because attribute arguments can't be decimal.
    [TestMethod]
    [DataRow("", "Furniture", 10.0, 1, "Name", DisplayName = "Empty name")]
    [DataRow("   ", "Furniture", 10.0, 1, "Name", DisplayName = "Whitespace-only name")]
    [DataRow(null, "Furniture", 10.0, 1, "Name", DisplayName = "Null name")]
    [DataRow("Chair", "", 10.0, 1, "Category", DisplayName = "Empty category")]
    [DataRow("Chair", "  ", 10.0, 1, "Category", DisplayName = "Whitespace-only category")]
    [DataRow("Chair", "Furniture", -0.01, 1, "Price", DisplayName = "Negative price")]
    [DataRow("Chair", "Furniture", 10.0, -1, "Stock", DisplayName = "Negative stock")]
    public async Task CreateAsync_InvalidInput_ReturnsInvalidWithoutCallingRepository(
        string? name, string category, double price, int stock, string expectedErrorField)
    {
        // Arrange
        var input = new ProductInput(name!, category, (decimal)price, stock);

        // Act
        var result = await _service.CreateAsync(input);

        // Assert
        Assert.AreEqual(ServiceResultStatus.Invalid, result.Status);
        Assert.IsTrue(result.Errors.ContainsKey(expectedErrorField), $"Expected an error for {expectedErrorField}.");
        _repository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyCacheNotCleared();
    }

    [TestMethod]
    public async Task CreateAsync_NameAndCategoryOverMaxLength_ReturnsInvalidWithoutCallingRepository()
    {
        // Arrange
        var input = new ProductInput(new string('n', Product.NameMaxLength + 1), new string('c', Product.CategoryMaxLength + 1), 1m, 1);

        // Act
        var result = await _service.CreateAsync(input);

        // Assert
        Assert.AreEqual(ServiceResultStatus.Invalid, result.Status);
        Assert.IsTrue(result.Errors.ContainsKey("Name"));
        Assert.IsTrue(result.Errors.ContainsKey("Category"));
        _repository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateAsync_PaddedValueWithinLimitAfterTrim_IsAccepted()
    {
        // Arrange: length is checked after trimming, so surrounding spaces don't count.
        _repository.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => WithId(p, 1));
        var input = new ProductInput("  " + new string('n', Product.NameMaxLength) + "  ", "Furniture", 1m, 1);

        // Act
        var result = await _service.CreateAsync(input);

        // Assert
        Assert.AreEqual(ServiceResultStatus.Success, result.Status);
    }

    // ---------- Update ----------

    [TestMethod]
    public async Task UpdateAsync_ExistingProduct_SavesTrimmedValuesAndReturnsSuccess()
    {
        // Arrange
        Product? saved = null;
        _repository.Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback((Product p, CancellationToken _) => saved = p)
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateAsync(7, new ProductInput(" Standing Desk XL ", " Furniture ", 450.00m, 2));

        // Assert
        Assert.AreEqual(ServiceResultStatus.Success, result.Status);
        Assert.AreEqual(new ProductDto(7, "Standing Desk XL", "Furniture", 450.00m, 2), result.Value);
        Assert.IsNotNull(saved);
        Assert.AreEqual(7, saved.ProductId);
        Assert.AreEqual("Standing Desk XL", saved.Name);
        Assert.AreEqual("Furniture", saved.Category);
    }

    [TestMethod]
    public async Task UpdateAsync_ExistingProduct_ClearsProductAndReportCache()
    {
        // Arrange
        _repository.Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        await _service.UpdateAsync(7, new ProductInput("Standing Desk", "Furniture", 400.00m, 3));

        // Assert
        VerifyCacheCleared(productId: 7);
    }

    [TestMethod]
    public async Task UpdateAsync_MissingProduct_ReturnsNotFoundAndKeepsCache()
    {
        // Arrange: the repository reports that no row matched the ID.
        _repository.Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _service.UpdateAsync(99, new ProductInput("Ghost", "Furniture", 1m, 1));

        // Assert
        Assert.AreEqual(ServiceResultStatus.NotFound, result.Status);
        Assert.IsNull(result.Value);
        _repository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyCacheNotCleared();
    }

    [TestMethod]
    public async Task UpdateAsync_InvalidInput_ReturnsInvalidWithoutCallingRepository()
    {
        // Arrange
        var input = new ProductInput("", "Furniture", -5m, 1);

        // Act
        var result = await _service.UpdateAsync(7, input);

        // Assert
        Assert.AreEqual(ServiceResultStatus.Invalid, result.Status);
        Assert.IsTrue(result.Errors.ContainsKey("Name"));
        Assert.IsTrue(result.Errors.ContainsKey("Price"));
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyCacheNotCleared();
    }

    // ---------- Delete ----------

    [TestMethod]
    public async Task DeleteAsync_ExistingProduct_ReturnsTrue()
    {
        // Arrange
        _repository.Setup(r => r.DeleteAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(7);

        // Assert
        Assert.IsTrue(result);
        _repository.Verify(r => r.DeleteAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task DeleteAsync_ExistingProduct_ClearsProductAndReportCache()
    {
        // Arrange
        _repository.Setup(r => r.DeleteAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        await _service.DeleteAsync(7);

        // Assert
        VerifyCacheCleared(productId: 7);
    }

    [TestMethod]
    public async Task DeleteAsync_MissingProduct_ReturnsFalseAndKeepsCache()
    {
        // Arrange
        _repository.Setup(r => r.DeleteAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(99);

        // Assert
        Assert.IsFalse(result);
        VerifyCacheNotCleared();
    }

    // ---------- Helpers ----------

    // Makes the cache mock run the loader delegate for this result type, as a cache miss would.
    private void PassThroughCachedLong<T>() =>
        _cache.Setup(c => c.CachedLongAsync(It.IsAny<string>(), It.IsAny<Func<Task<T>>>()))
            .Returns((string _, Func<Task<T>> loadData) => loadData());

    private void PassThroughCached<T>() =>
        _cache.Setup(c => c.CachedAsync(It.IsAny<string>(), It.IsAny<Func<Task<T>>>()))
            .Returns((string _, Func<Task<T>> loadData) => loadData());

    // After a successful write, every entry the write can make stale (PLAN.md §4.2) is removed.
    private void VerifyCacheCleared(int productId)
    {
        _cache.Verify(c => c.Remove(CacheKeys.AllProducts), Times.Once);
        _cache.Verify(c => c.Remove(CacheKeys.Categories), Times.Once);
        _cache.Verify(c => c.Remove(CacheKeys.Product(productId)), Times.Once);
        _cache.Verify(c => c.Remove(CacheKeys.AveragePriceByCategory), Times.Once);
        _cache.Verify(c => c.Remove(CacheKeys.HighestStockValueCategory), Times.Once);
    }

    private void VerifyCacheNotCleared() =>
        _cache.Verify(c => c.Remove(It.IsAny<string>()), Times.Never);

    private static Product NewProduct(int id, string name, string category, decimal price, int stock) =>
        new() { ProductId = id, Name = name, Category = category, Price = price, Stock = stock };

    // Simulates the database assigning the identity value on insert.
    private static Product WithId(Product product, int id)
    {
        product.ProductId = id;
        return product;
    }
}
