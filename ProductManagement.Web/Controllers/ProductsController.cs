using Microsoft.AspNetCore.Mvc;
using ProductManagement.Web.Models.ViewModels;
using ProductManagement.Web.Services;

namespace ProductManagement.Web.Controllers;

/// <summary>
/// The Product List page and the AJAX endpoints its script calls. Handles only HTTP:
/// all rules and data access sit behind <see cref="IProductService"/>.
///
/// AJAX responses use status codes, so products.js can tell the outcomes apart:
///   200 + table rows  the save or delete worked
///   422 + form        validation failed; the form comes back with its messages
///   404               the product no longer exists
///   400               bad request, including a missing or invalid anti-forgery token
/// </summary>
public class ProductsController : Controller
{
    private const string RowsPartial = "_ProductRows";
    private const string FormPartial = "_ProductForm";

    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    // GET /Products
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllAsync(cancellationToken);
        return View(products);
    }

    // GET /Products/Rows: the table body only, used to refresh the table after a "not found".
    [HttpGet]
    public Task<IActionResult> Rows(CancellationToken cancellationToken) => RowsAsync(cancellationToken);

    // GET /Products/Create: an empty form for the modal.
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new ProductFormViewModel
        {
            Categories = await _productService.GetCategoriesAsync(cancellationToken),
        };
        return PartialView(FormPartial, model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await FormWithErrorsAsync(model, cancellationToken);
        }

        var result = await _productService.CreateAsync(model.ToInput(), cancellationToken);
        if (result.Status == ServiceResultStatus.Invalid)
        {
            AddServiceErrors(result.Errors);
            return await FormWithErrorsAsync(model, cancellationToken);
        }

        return await RowsAsync(cancellationToken);
    }

    // GET /Products/Edit/5: the form filled with the product's values.
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var model = ProductFormViewModel.FromDto(product);
        model.Categories = await _productService.GetCategoriesAsync(cancellationToken);
        return PartialView(FormPartial, model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model, CancellationToken cancellationToken)
    {
        // The ID comes from the route only, so the form can't redirect the update to another product.
        model.ProductId = id;

        if (!ModelState.IsValid)
        {
            return await FormWithErrorsAsync(model, cancellationToken);
        }

        var result = await _productService.UpdateAsync(id, model.ToInput(), cancellationToken);
        switch (result.Status)
        {
            case ServiceResultStatus.NotFound:
                return NotFound();
            case ServiceResultStatus.Invalid:
                AddServiceErrors(result.Errors);
                return await FormWithErrorsAsync(model, cancellationToken);
            default:
                return await RowsAsync(cancellationToken);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _productService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return await RowsAsync(cancellationToken);
    }

    private async Task<IActionResult> RowsAsync(CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllAsync(cancellationToken);
        return PartialView(RowsPartial, products);
    }

    // Re-renders the form with its validation messages and status 422.
    private async Task<IActionResult> FormWithErrorsAsync(ProductFormViewModel model, CancellationToken cancellationToken)
    {
        model.Categories = await _productService.GetCategoriesAsync(cancellationToken);
        var view = PartialView(FormPartial, model);
        view.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return view;
    }

    // The service's error keys are the field names, so each message appears next to its field.
    private void AddServiceErrors(IReadOnlyDictionary<string, string> errors)
    {
        foreach (var (field, message) in errors)
        {
            ModelState.AddModelError(field, message);
        }
    }
}
