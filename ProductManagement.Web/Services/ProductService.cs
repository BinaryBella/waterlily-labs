using ProductManagement.Web.Models.Dtos;
using ProductManagement.Web.Models.Entities;
using ProductManagement.Web.Repositories;
using ProductManagement.Web.Services.Caching;

namespace ProductManagement.Web.Services;

/// <summary>
/// Callback invoked after a product is created, updated or deleted.
/// It returns void on purpose: a multicast delegate only hands back the last handler's
/// return value, so with Task-returning handlers the earlier tasks would go unawaited and
/// their errors would be lost. The handlers only remove cache entries, which is synchronous.
/// </summary>
public delegate void ProductChangedHandler(int productId);

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<ProductService> _logger;

    // Multicast: invoking it runs every handler added in the constructor, in order.
    private readonly ProductChangedHandler _productChanged;

    public ProductService(IProductRepository repository, ICacheService cache, ILogger<ProductService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;

        // Each handler clears one group of entries that a write makes stale.
        _productChanged = RemoveProductEntries;
        _productChanged += RemoveReportEntries;
    }

    public Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // The lambda is the cache-miss callback: it only runs when the list isn't cached.
        // No expiry, because every write removes this entry.
        return _cache.CachedLongAsync<IReadOnlyList<ProductDto>>(CacheKeys.AllProducts, async () =>
        {
            var products = await _repository.GetAllAsync(cancellationToken);
            return products.Select(ToDto).ToList().AsReadOnly();
        });
    }

    public Task<ProductDto?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        // A missing product loads as null, which the cache doesn't store.
        return _cache.CachedAsync(CacheKeys.Product(productId), async () =>
        {
            var product = await _repository.GetByIdAsync(productId, cancellationToken);
            return product is null ? null : ToDto(product);
        });
    }

    public Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return _cache.CachedLongAsync<IReadOnlyList<string>>(CacheKeys.Categories, async () =>
        {
            var categories = await _repository.GetCategoriesAsync(cancellationToken);
            return categories.ToList().AsReadOnly();
        });
    }

    public async Task<ServiceResult<ProductDto>> CreateAsync(ProductInput input, CancellationToken cancellationToken = default)
    {
        var (name, category) = Normalize(input);
        var errors = Validate(name, category, input.Price, input.Stock);
        if (errors.Count > 0)
        {
            return ServiceResult<ProductDto>.Invalid(errors);
        }

        var product = new Product { Name = name, Category = category, Price = input.Price, Stock = input.Stock };
        var saved = await _repository.AddAsync(product, cancellationToken);

        _productChanged(saved.ProductId);
        _logger.LogInformation("Created product {ProductId}", saved.ProductId);

        return ServiceResult<ProductDto>.Success(ToDto(saved));
    }

    public async Task<ServiceResult<ProductDto>> UpdateAsync(int productId, ProductInput input, CancellationToken cancellationToken = default)
    {
        var (name, category) = Normalize(input);
        var errors = Validate(name, category, input.Price, input.Stock);
        if (errors.Count > 0)
        {
            return ServiceResult<ProductDto>.Invalid(errors);
        }

        var product = new Product { ProductId = productId, Name = name, Category = category, Price = input.Price, Stock = input.Stock };
        var updated = await _repository.UpdateAsync(product, cancellationToken);
        if (!updated)
        {
            // Deleted by someone else since the form was opened. Nothing changed, so the cache stays.
            return ServiceResult<ProductDto>.NotFound();
        }

        _productChanged(productId);
        _logger.LogInformation("Updated product {ProductId}", productId);

        return ServiceResult<ProductDto>.Success(ToDto(product));
    }

    public async Task<bool> DeleteAsync(int productId, CancellationToken cancellationToken = default)
    {
        var deleted = await _repository.DeleteAsync(productId, cancellationToken);
        if (!deleted)
        {
            return false;
        }

        _productChanged(productId);
        _logger.LogInformation("Deleted product {ProductId}", productId);

        return true;
    }

    // ProductChangedHandler handlers. Any product write can change the list, the category
    // values, that product's own entry and both report results.

    private void RemoveProductEntries(int productId)
    {
        _cache.Remove(CacheKeys.AllProducts);
        _cache.Remove(CacheKeys.Categories);
        _cache.Remove(CacheKeys.Product(productId));
    }

    private void RemoveReportEntries(int productId)
    {
        _cache.Remove(CacheKeys.AveragePriceByCategory);
        _cache.Remove(CacheKeys.HighestStockValueCategory);
    }

    // Category is free text and the reports group on it, so stray spaces would split one
    // category into two. Model binding can also pass null for an empty field.
    private static (string Name, string Category) Normalize(ProductInput input) =>
        (input.Name?.Trim() ?? string.Empty, input.Category?.Trim() ?? string.Empty);

    // The same rules the form checks in the browser, applied again here so nothing invalid
    // reaches the database, whatever the client sends.
    private static Dictionary<string, string> Validate(string name, string category, decimal price, int stock)
    {
        var errors = new Dictionary<string, string>();

        if (name.Length == 0)
            errors[nameof(ProductInput.Name)] = "Name is required.";
        else if (name.Length > Product.NameMaxLength)
            errors[nameof(ProductInput.Name)] = $"Name must be {Product.NameMaxLength} characters or fewer.";

        if (category.Length == 0)
            errors[nameof(ProductInput.Category)] = "Category is required.";
        else if (category.Length > Product.CategoryMaxLength)
            errors[nameof(ProductInput.Category)] = $"Category must be {Product.CategoryMaxLength} characters or fewer.";

        if (price < 0)
            errors[nameof(ProductInput.Price)] = "Price can't be negative.";

        if (stock < 0)
            errors[nameof(ProductInput.Stock)] = "Stock can't be negative.";

        return errors;
    }

    private static ProductDto ToDto(Product product) =>
        new(product.ProductId, product.Name, product.Category, product.Price, product.Stock);
}
