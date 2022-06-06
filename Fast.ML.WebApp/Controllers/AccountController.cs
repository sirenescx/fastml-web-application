using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using Fast.ML.WebApp.Extensions;
using Fast.ML.WebApp.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fast.ML.WebApp.Controllers;

[Authorize(AuthenticationSchemes = "Cookies, Google")]
public class AccountController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountController(IWebHostEnvironment environment, 
        IHttpContextAccessor httpContextAccessor)
    {
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
    }

    public IActionResult Index([FromQuery(Name = "user_id")] int userId)
    {
        var systemUserId = Convert.ToInt32(_httpContextAccessor.GetClaimValue(ClaimTypes.Sid));
        if (userId != systemUserId)
        {
            return RedirectToAction("Index",
                new {user_id = systemUserId});
        }
        
        var algorithms = new List<string>();
        var path = FileUtils.GetUserFolder(_environment.WebRootPath, systemUserId);
        var folders = new DirectoryInfo(path).GetDirectories();
        foreach (var folder in folders)
        {
            foreach (var file in folder.GetFiles())
            {
                if (file.Name.EndsWith(".pickle"))
                {
                    algorithms.Add(
                        string.Join(
                            "\t", 
                            folder.Name,
                            "alg_" + file.Name.ToCamelCase()
                                .Replace(".pickle", string.Empty)));
                }
            }
        }

        ViewBag.Algorithms = algorithms;
        
        return View(userId);
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeAttribute : Microsoft.AspNetCore.Authorization.AuthorizeAttribute, 
    IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity is {IsAuthenticated: false})
        {
            context.Result = new UnauthorizedResult();
        }
    }
}