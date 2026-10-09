using ProductManagement.Web.Models.Dtos;

namespace ProductManagement.Web.Models.ViewModels;

/// <summary>The Report page: both calculations, as returned by the stored procedures.</summary>
public class ReportViewModel
{
    /// <summary>One row per category, sorted by category name.</summary>
    public IReadOnlyList<CategoryAveragePrice> AveragePrices { get; init; } = Array.Empty<CategoryAveragePrice>();

    /// <summary>The top category by total Price x Stock. More than one entry means they share the top value.</summary>
    public IReadOnlyList<CategoryStockValue> HighestStockValue { get; init; } = Array.Empty<CategoryStockValue>();

    /// <summary>Both procedures return no rows when there are no products.</summary>
    public bool HasData => AveragePrices.Count > 0;

    public bool IsTie => HighestStockValue.Count > 1;
}
