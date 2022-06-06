using System.Diagnostics;
using Fast.ML.WebApp.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Fast.ML.WebApp.Controllers;

public class HomeController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly  IHttpContextAccessor _httpContextAccessor;

    public HomeController(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _environment = environment;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("policy")]
    public IActionResult Privacy()
    {
        return View();
    }
    
    [HttpGet("contacts")]
    public IActionResult Developer()
    {
        return View();
    }

    [HttpGet("get_started")]
    public IActionResult GetStarted()
    {
        return View();
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}