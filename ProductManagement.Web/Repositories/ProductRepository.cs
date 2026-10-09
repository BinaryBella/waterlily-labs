using Microsoft.EntityFrameworkCore;
using ProductManagement.Web.Data;
using ProductManagement.Web.Models.Dtos;
using ProductManagement.Web.Models.Entities;

namespace ProductManagement.Web.Repositories;

/// <summary>EF Core implementation of <see cref="IProductRepository"/>.</summary>
public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db)
    {
        _db = db;
    }

    // Reads use AsNoTracking: the results are only displayed or cached, never saved back
    // through this context, so change tracking would be wasted work.

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ThenBy(p => p.ProductId)
            .ToListAsync(cancellationToken);
    }

    public Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);
    }

    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        // EF Core has filled ProductId with the identity value. Detach the entity so this
        // scoped context doesn't keep tracking an object the caller now owns.
        _db.Entry(product).State = EntityState.Detached;
        return product;
    }

    public async Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        // A single UPDATE ... WHERE ProductId = @id, with no read first. The affected row count
        // tells us whether the product still exists.
        var rows = await _db.Products
            .Where(p => p.ProductId == product.ProductId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Name, product.Name)
                .SetProperty(p => p.Category, product.Category)
                .SetProperty(p => p.Price, product.Price)
                .SetProperty(p => p.Stock, product.Stock),
                cancellationToken);

        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int productId, CancellationToken cancellationToken = default)
    {
        // A single DELETE ... WHERE ProductId = @id. Zero affected rows means it was already gone.
        var rows = await _db.Products
            .Where(p => p.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);

        return rows > 0;
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Products
            .AsNoTracking()
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    // The report calculations run inside the stored procedures. SqlQuery maps each result
    // column to the record property of the same name. Nothing is composed on top of the
    // query, because SQL Server can't wrap an EXEC in a subquery.

    public async Task<IReadOnlyList<CategoryAveragePrice>> GetAveragePriceByCategoryAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Database
            .SqlQuery<CategoryAveragePrice>($"EXEC dbo.usp_GetAveragePriceByCategory")
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryStockValue>> GetHighestStockValueCategoryAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Database
            .SqlQuery<CategoryStockValue>($"EXEC dbo.usp_GetHighestStockValueCategory")
            .ToListAsync(cancellationToken);
    }
}
