using ProductManagement.Web.Models.Dtos;

namespace ProductManagement.Web.Services;

/// <summary>The two category reports. The calculations run in stored procedures; results are cached for 5 minutes.</summary>
public interface IReportService
{
    /// <summary>Average price and product count per category, sorted by category. Empty when there are no products.</summary>
    Task<IReadOnlyList<CategoryAveragePrice>> GetAveragePriceByCategoryAsync(CancellationToken cancellationToken = default);

    /// <summary>The category with the highest total Price x Stock. More than one entry means a tie; empty when there are no products.</summary>
    Task<IReadOnlyList<CategoryStockValue>> GetHighestStockValueCategoryAsync(CancellationToken cancellationToken = default);
}
