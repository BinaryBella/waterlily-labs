using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ProductManagement.Web.Models.Dtos;
using ProductManagement.Web.Models.Entities;

namespace ProductManagement.Web.Models.ViewModels;

/// <summary>
/// The Add/Edit product form. The data annotations drive jQuery Validation in the browser and
/// model validation on the server. <c>ProductService</c> checks the same rules again, so they
/// hold even for a request that doesn't come from this form.
/// </summary>
public class ProductFormViewModel
{
    /// <summary>Null when adding, set when editing. Taken from the route, never bound from the form.</summary>
    [BindNever]
    public int? ProductId { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(Product.NameMaxLength, ErrorMessage = "Name must be {1} characters or fewer.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(Product.CategoryMaxLength, ErrorMessage = "Category must be {1} characters or fewer.")]
    public string? Category { get; set; }

    // Nullable so an empty field reports "required" instead of a type-conversion error.
    // The upper bound is the largest value decimal(18,2) can hold.
    [Required(ErrorMessage = "Price is required.")]
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Price must be between 0 and 9,999,999,999,999,999.99.")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "Stock is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock can't be negative.")]
    public int? Stock { get; set; }

    /// <summary>Existing categories offered as autocomplete suggestions. Display only.</summary>
    [BindNever]
    [ValidateNever]
    public IReadOnlyList<string> Categories { get; set; } = Array.Empty<string>();

    public bool IsEdit => ProductId.HasValue;

    public static ProductFormViewModel FromDto(ProductDto product) => new()
    {
        ProductId = product.ProductId,
        Name = product.Name,
        Category = product.Category,
        Price = product.Price,
        Stock = product.Stock,
    };

    /// <summary>Call only after model validation has passed, so Price and Stock have values.</summary>
    public ProductInput ToInput() => new(Name ?? string.Empty, Category ?? string.Empty, Price!.Value, Stock!.Value);
}
