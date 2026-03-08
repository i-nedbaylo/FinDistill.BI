using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FinDistill.Web.Models;

namespace FinDistill.Web.Controllers;

/// <summary>
/// Fallback controller for error display and privacy pages.
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>Returns the home index view (redirected to Dashboard in production).</summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>Returns the privacy policy view.</summary>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>Returns the error view populated with the current request identifier.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
