namespace ProductManagement.Web.Models.Dtos;

/// <summary>
/// One row of <c>dbo.usp_GetAveragePriceByCategory</c>. EF Core maps the procedure's columns
/// to these properties by name at runtime, so the names must match the SQL exactly.
/// </summary>
public sealed record CategoryAveragePrice(string Category, decimal AveragePrice, int ProductCount);
