using Microsoft.AspNetCore.Mvc;
using ProductManagement.Web.Models.ViewModels;
using ProductManagement.Web.Services;

namespace ProductManagement.Web.Controllers;

/// <summary>The Report page. The calculations run in stored procedures behind <see cref="IReportService"/>.</summary>
public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    // GET /Reports
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        // Awaited one after the other: both calls share the request's DbContext, which doesn't
        // allow two queries at the same time.
        var model = new ReportViewModel
        {
            AveragePrices = await _reportService.GetAveragePriceByCategoryAsync(cancellationToken),
            HighestStockValue = await _reportService.GetHighestStockValueCategoryAsync(cancellationToken),
        };

        return View(model);
    }
}
