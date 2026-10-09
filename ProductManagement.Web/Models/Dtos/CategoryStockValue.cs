namespace ProductManagement.Web.Models.Dtos;

/// <summary>
/// One row of <c>dbo.usp_GetHighestStockValueCategory</c>: a category and the total of
/// Price x Stock across its products. EF Core maps columns to properties by name, so the
/// names must match the SQL exactly.
/// </summary>
public sealed record CategoryStockValue(string Category, decimal TotalStockValue);
