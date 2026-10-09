using ProductManagement.Web.Models.Dtos;
using ProductManagement.Web.Repositories;
using ProductManagement.Web.Services.Caching;

namespace ProductManagement.Web.Services;

/// <summary>
/// Returns the report results from the repository through the 5-minute cache.
/// No calculation happens here. <see cref="ProductService"/> removes these entries after
/// every product write, so the 5 minutes is only an upper bound on staleness.
/// </summary>
public class ReportService : IReportService
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;

    public ReportService(IProductRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public Task<IReadOnlyList<CategoryAveragePrice>> GetAveragePriceByCategoryAsync(CancellationToken cancellationToken = default)
    {
        return _cache.CachedAsync<IReadOnlyList<CategoryAveragePrice>>(CacheKeys.AveragePriceByCategory, async () =>
        {
            var rows = await _repository.GetAveragePriceByCategoryAsync(cancellationToken);
            return rows.ToList().AsReadOnly();
        });
    }

    public Task<IReadOnlyList<CategoryStockValue>> GetHighestStockValueCategoryAsync(CancellationToken cancellationToken = default)
    {
        return _cache.CachedAsync<IReadOnlyList<CategoryStockValue>>(CacheKeys.HighestStockValueCategory, async () =>
        {
            var rows = await _repository.GetHighestStockValueCategoryAsync(cancellationToken);
            return rows.ToList().AsReadOnly();
        });
    }
}
