namespace ProductManagement.Web.Models.Entities;

/// <summary>
/// A product stored in the Products table. This is a plain class: column types,
/// lengths and constraints are mapped in <c>Data/Configurations/ProductConfiguration</c>.
/// </summary>
public class Product
{
    /// <summary>Column length of Name. Shared by the database mapping and the service validation.</summary>
    public const int NameMaxLength = 100;

    /// <summary>Column length of Category. Shared by the database mapping and the service validation.</summary>
    public const int CategoryMaxLength = 50;

    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Free-text category. There is no category table, so reports group on this value.</summary>
    public string Category { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }
}
