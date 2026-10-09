namespace ProductManagement.Web.Services.Caching;

/// <summary>
/// Every cache key in one place, so a read and its invalidation can't use different keys.
/// </summary>
public static class CacheKeys
{
    /// <summary>The full product list (no expiry).</summary>
    public const string AllProducts = "products:all";

    /// <summary>The distinct category values for the Category autocomplete (no expiry).</summary>
    public const string Categories = "products:categories";

    /// <summary>The average-price-by-category report (5 minutes).</summary>
    public const string AveragePriceByCategory = "reports:average-price-by-category";

    /// <summary>The highest-stock-value-category report (5 minutes).</summary>
    public const string HighestStockValueCategory = "reports:highest-stock-value-category";

    private const string ProductPrefix = "products:id:";

    /// <summary>A single product by ID (5 minutes). The key includes the ID, so it's built rather than constant.</summary>
    public static string Product(int productId) => ProductPrefix + productId;
}
