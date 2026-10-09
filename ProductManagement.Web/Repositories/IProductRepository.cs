using ProductManagement.Web.Models.Dtos;
using ProductManagement.Web.Models.Entities;

namespace ProductManagement.Web.Repositories;

/// <summary>
/// The only data access in the application. It reads and writes products and runs the
/// report procedures. Validation, caching and mapping to DTOs belong to the services.
/// </summary>
public interface IProductRepository
{
    /// <summary>All products, sorted by name.</summary>
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>The product with this ID, or <c>null</c> if it doesn't exist.</summary>
    Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>Inserts the product and returns it with its generated <see cref="Product.ProductId"/>.</summary>
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>Saves the product's values over the stored row. Returns <c>false</c> if the row doesn't exist.</summary>
    Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>Deletes the product. Returns <c>false</c> if the row doesn't exist.</summary>
    Task<bool> DeleteAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>The distinct category values in use, sorted A to Z, for the Category autocomplete.</summary>
    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs <c>dbo.usp_GetAveragePriceByCategory</c>.</summary>
    Task<IReadOnlyList<CategoryAveragePrice>> GetAveragePriceByCategoryAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs <c>dbo.usp_GetHighestStockValueCategory</c>. More than one row means a tie.</summary>
    Task<IReadOnlyList<CategoryStockValue>> GetHighestStockValueCategoryAsync(CancellationToken cancellationToken = default);
}
