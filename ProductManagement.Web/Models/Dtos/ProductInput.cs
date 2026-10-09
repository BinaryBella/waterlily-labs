namespace ProductManagement.Web.Models.Dtos;

/// <summary>
/// The values a user submits to create or update a product. The ID isn't part of it:
/// the database generates it on create, and update takes it as a separate argument.
/// The service trims and validates these values before anything is saved.
/// </summary>
public sealed record ProductInput(string Name, string Category, decimal Price, int Stock);
