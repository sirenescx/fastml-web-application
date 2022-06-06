using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fast.ML.WebApp.Extensions;
using Fast.ML.WebApp.Models;
using Fast.ML.WebApp.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Fast.ML.WebApp.Controllers;

public class GoogleLoginController : Controller
{
    private const string ApiHttpClientName = "ApiHttpClient";
    private readonly HttpClient _apiHttpClient;
    private readonly IWebHostEnvironment _environment;

    public GoogleLoginController(IHttpClientFactory httpClientFactory, 
        IWebHostEnvironment environment)
    {
        _apiHttpClient = httpClientFactory.CreateClient(ApiHttpClientName);
        _environment = environment;
    }
    
    public IActionResult Index() =>
        new ChallengeResult(
            GoogleDefaults.AuthenticationScheme,
            new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse", "GoogleLogin") 
            });

    public async Task<IActionResult> GoogleResponse()
    {
        var authenticateResult = await HttpContext
            .AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded)
            return BadRequest();

        if (authenticateResult.Principal == null) 
            return RedirectToAction("Index", "Home");
        
        var claimsIdentity = new ClaimsIdentity(
            CookieAuthenticationDefaults.AuthenticationScheme);
        claimsIdentity.AddClaim(
            authenticateResult.Principal, ClaimTypes.Email);
        claimsIdentity.AddClaim(
            authenticateResult.Principal, ClaimTypes.GivenName);
        claimsIdentity.AddClaim(
            authenticateResult.Principal, ClaimTypes.Surname);

        var userId = await GetUserId(authenticateResult.Principal);
        claimsIdentity.AddClaim(ClaimTypes.Sid, userId);

        var userFolder = FileUtils.GetUserFolder(
            _environment.WebRootPath, userId!.Value);
        if (!Directory.Exists(userFolder))
            Directory.CreateDirectory(userFolder!);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));

        return RedirectToAction("Index", "Model");

    }

    [Authorize]
    public async Task<IActionResult> SignOutFromGoogleLogin()
    {
        if (HttpContext.Request.Cookies.Count > 0)
        {
            var siteCookies = 
                HttpContext.Request.Cookies.Where(c =>
                c.Key.Contains(".AspNetCore.") || c.Key.Contains("Microsoft.Authentication"));
            foreach (var cookie in siteCookies)
                Response.Cookies.Delete(cookie.Key);
        }
        
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public async Task<int?> GetUserId(ClaimsPrincipal claimsPrincipal)
    {
        var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value;
        var firstName = claimsPrincipal.FindFirst(ClaimTypes.GivenName)?.Value;
        var lastName = claimsPrincipal.FindFirst(ClaimTypes.Surname)?.Value;
        
        var query = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["FirstName"] = firstName,
            ["LastName"] = lastName
        };
        
        var requestUri = new Uri(_apiHttpClient?.BaseAddress ?? 
                                 new Uri(string.Empty), "users");
        var requestUriWithParameters = QueryHelpers
            .AddQueryString(requestUri.AbsoluteUri, query);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(requestUriWithParameters)
        };

        var response = await _apiHttpClient?.SendAsync(request, CancellationToken.None)!;
        return response.Content.ReadFromJsonAsync<UserIdResponse>().Result?.Id;
    }
}