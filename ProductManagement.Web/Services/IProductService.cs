using ProductManagement.Web.Models.Dtos;

namespace ProductManagement.Web.Services;

/// <summary>Product CRUD for the controllers. Reads are cached; writes clear the affected cache entries.</summary>
public interface IProductService
{
    /// <summary>All products, sorted by name.</summary>
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>The product with this ID, or <c>null</c> if it doesn't exist.</summary>
    Task<ProductDto?> GetByIdAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>The distinct categories in use, sorted A to Z, for the Category autocomplete.</summary>
    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Trims and validates the input, then saves a new product. Returns Success or Invalid.</summary>
    Task<ServiceResult<ProductDto>> CreateAsync(ProductInput input, CancellationToken cancellationToken = default);

    /// <summary>Trims and validates the input, then updates the product. Returns Success, Invalid or NotFound.</summary>
    Task<ServiceResult<ProductDto>> UpdateAsync(int productId, ProductInput input, CancellationToken cancellationToken = default);

    /// <summary>Deletes the product. Returns <c>false</c> if it doesn't exist.</summary>
    Task<bool> DeleteAsync(int productId, CancellationToken cancellationToken = default);
}
