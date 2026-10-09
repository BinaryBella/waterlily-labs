namespace ProductManagement.Web.Models.Dtos;

/// <summary>
/// A product as the services return it. It is immutable, so a cached instance can't be
/// changed by one caller and then seen changed by the next.
/// </summary>
public sealed record ProductDto(int ProductId, string Name, string Category, decimal Price, int Stock);
