using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ProductManagement.Web.Controllers;

/// <summary>
/// Target of the exception handler outside Development. The middleware has already logged the
/// exception by the time this runs. The page shows a request ID that can be matched to that log entry.
/// </summary>
public class ErrorController : Controller
{
    [Route("/Error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index()
    {
        ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View("Error");
    }
}
