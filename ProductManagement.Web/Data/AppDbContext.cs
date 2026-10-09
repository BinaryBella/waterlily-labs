using Microsoft.EntityFrameworkCore;
using ProductManagement.Web.Data.Configurations;
using ProductManagement.Web.Models.Entities;

namespace ProductManagement.Web.Data;

/// <summary>
/// EF Core context for the application. Only the repository layer uses it;
/// controllers and views never reference it.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
    }
}
