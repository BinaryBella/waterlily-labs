using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagement.Web.Models.Entities;

namespace ProductManagement.Web.Data.Configurations;

/// <summary>Fluent API mapping for <see cref="Product"/> to the Products table.</summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // The check constraints back up the service-layer validation, so negative values
        // are rejected even if a row is written outside the application.
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint("CK_Products_Price_NonNegative", "[Price] >= 0");
            table.HasCheckConstraint("CK_Products_Stock_NonNegative", "[Stock] >= 0");
        });

        builder.HasKey(p => p.ProductId);
        builder.Property(p => p.ProductId).UseIdentityColumn();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Stock)
            .IsRequired();

        // Both report procedures group by Category.
        builder.HasIndex(p => p.Category)
            .HasDatabaseName("IX_Products_Category");
    }
}
