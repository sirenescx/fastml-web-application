using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fast.ML.WebApp.Extensions;
using Fast.ML.WebApp.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fast.ML.WebApp.Controllers;

[Authorize]
[Route("task/")]
public class TaskController : Controller
{
    private const string ApiHttpClientName = "ApiHttpClient";

    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HttpClient _apiHttpClient;

    public TaskController(
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory)
    {
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
        _apiHttpClient = httpClientFactory.CreateClient(ApiHttpClientName);
    }

    public IActionResult Index()
    {
        return View();
    }

    [Route("/tasks")]
    public async Task<IActionResult> Tasks([FromQuery(Name = "user_id")] int userId)
    {
        var systemUserId = Convert.ToInt32(_httpContextAccessor
            .GetClaimValue(ClaimTypes.Sid));
        
        if (userId != systemUserId)
        {
            return RedirectToAction("Tasks",
                new {user_id = systemUserId});
        }

        var requestUri = new Uri(_apiHttpClient?.BaseAddress ?? 
                                 new Uri(string.Empty), "tasks");
        var query = new Dictionary<string, string>
        {
            ["userId"] = userId.ToString()
        };

        var requestUriWithParameters = QueryHelpers
            .AddQueryString(requestUri.AbsoluteUri, query);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(requestUriWithParameters)
        };

        var response = await _apiHttpClient?.SendAsync(
                request, CancellationToken.None)!.Result.Content
            .ReadAsStringAsync();
        
        var taskIds = JsonConvert
            .DeserializeObject<JObject>(response)["taskIds"]
            .Select(token => token.ToString());

        ViewBag.Tasks = taskIds.OrderByDescending(token => token).ToList();
        Console.WriteLine();

        return View();
    }
    
    [Route("log")]
    public IActionResult Log([FromQuery(Name = "task_id")] string taskId)
    {
        return View();
    }

    [HttpGet("read_data")]
    public ActionResult ReadData([FromQuery(Name = "task_id")] string taskId)
    {
        var userId = Convert.ToInt32(_httpContextAccessor
            .GetClaimValue(ClaimTypes.Sid));
        var logsPath = Path.Combine(
            FileUtils.GetUserFolder(_environment.WebRootPath, userId), taskId, "log");
        using var fs = new FileStream(
            logsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);
        var logLines = new List<string>();
        while (!sr.EndOfStream)
        {
            logLines.Add(sr.ReadLine());
        }
        return Content(string.Join(" <br> ", logLines));
    }
}