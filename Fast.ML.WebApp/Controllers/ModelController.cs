using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Fast.ML.WebApp.Configuration;
using Fast.ML.WebApp.Extensions;
using Fast.ML.WebApp.Models.ApiRequests;
using Fast.ML.WebApp.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fast.ML.WebApp.Controllers;

[Authorize(AuthenticationSchemes = "Cookies, Google")]
public class ModelController : Controller
{
    private const string ApiHttpClientName = "ApiHttpClient";

    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HttpClient _apiHttpClient;

    public ModelController(
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

    [HttpPost("model/upload_files")]
    public async Task<IActionResult> UploadTrainingFile(IFormFile trainingFile, IFormCollection data)
    {
        var separator = data.GetValue(Separator);
        var hasIndex = data.GetValueWithComparison(NoIndex, Off);

        if (trainingFile is null)
        {
            ViewData[ErrorMessage] = GetErrorAlertHTML("File for training was not uploaded.");
            return View("Index");
        }

        var userId = Convert.ToInt32(_httpContextAccessor.GetClaimValue(ClaimTypes.Sid));
        var folder = FileUtils.GetUserFolder(_environment.WebRootPath, userId);
        var fileNamePrefix = FileUtils.GetFileNamePrefix(_httpContextAccessor.HttpContext?.Session.Id, DateTime.Now);

        if (!FileUtils.IsExcelFile(trainingFile.FileName))
        {
            if (string.IsNullOrEmpty(separator))
            {
                ViewData[ErrorMessage] = GetErrorAlertHTML("Separator is required for .csv files.");
                return View("Index");
            }

            if (!separator.IsChar())
            {
                ViewData[ErrorMessage] = GetErrorAlertHTML("Separator could be one char only.");
                return View("Index");
            }
        }

        var trainingFileExtension = FileUtils.GetFileExtension(trainingFile.FileName);
        var trainingFileUploadResult = await SaveFile(
            folder,
            trainingFile,
            FileUtils.GetServerFileName(fileNamePrefix, trainingFileExtension));

        if (!string.IsNullOrEmpty(trainingFileUploadResult))
            return RedirectToAction(
                "Process",
                new {prefix = fileNamePrefix, separator = separator, index = Convert.ToInt32(!hasIndex)});
        
        ViewData[ErrorMessage] = GetErrorAlertHTML("File for training was not uploaded.");
        return View("Index");
    }

    public IActionResult Process(string prefix, string separator, int index)
    {
        var userId = Convert.ToInt32(_httpContextAccessor.GetClaimValue(ClaimTypes.Sid));
        var files = FileUtils.FindSessionFiles(_environment.WebRootPath, userId, prefix);
        if (files.Length == 0)
        {
            return RedirectToAction("Index");
        }
        var trainingFileName = files.First().FullName;

        string[] trainingFeatures;

        if (!string.IsNullOrEmpty(separator))
        {
            var delimiter = separator.First();
            trainingFeatures = ExtractFeatureNamesFromCsv(trainingFileName, delimiter, index);
            ViewBag.Delimiter = separator;
        }
        else
        {
            if (!FileUtils.IsExcelFile(trainingFileName))
            {
                ViewData[ErrorMessage] = GetErrorAlertHTML("Training dataset should contain at least two columns.");
                return View("Index");
            }
            trainingFeatures = ExtractFeatureNamesFromExcel(trainingFileName, index);
        }

        if (trainingFeatures.Length < 2)
        {
            ViewData[ErrorMessage] = GetErrorAlertHTML("Training dataset should contain at least two columns.");
            return View("Index");
        }

        ViewBag.TrainFeatureNames = trainingFeatures;
        ViewBag.Prefix = prefix;
        ViewBag.Index = index;

        GetAlgorithmsHtml();
        return View();
    }

    [HttpGet]
    [Route("upload_file_for_prediction")]
    public Task<IActionResult> Predict([FromQuery] string algorithm, [FromQuery] string prefix)
    {
        return Task.FromResult<IActionResult>(View());
    }
    
    [HttpPost]
    [Route("upload_file_for_prediction")]
    public async Task<IActionResult> UploadPredictionFile(
        IFormFile predictionFile, 
        IFormCollection data)
    {
        var separator = data.GetValue(Separator);
        var taskId = data.GetValue(Prefix);
        var algorithms = data.GetValuesByPrefix(AlgorithmPrefix);
        var hasIndex = data.GetValueWithComparison(NoIndex, Off);

        var userId = Convert.ToInt32(_httpContextAccessor.GetClaimValue(ClaimTypes.Sid));
        var folder = FileUtils.GetUserFolder(_environment.WebRootPath, userId);
        var fileNamePrefix = FileUtils.GetFileNamePrefix(_httpContextAccessor.HttpContext?.Session.Id, DateTime.Now);

        if (!FileUtils.IsExcelFile(predictionFile.FileName))
        {
            if (string.IsNullOrEmpty(separator))
            {
                ViewData[ErrorMessage] = GetErrorAlertHTML("Separator is required for .csv files.");
                return View("Index");
            }

            if (!separator.IsChar())
            {
                ViewData[ErrorMessage] = GetErrorAlertHTML("Separator could be one char only.");
                return View("Index");
            }
        }

        var predictionFileExtension = FileUtils.GetFileExtension(predictionFile.FileName);

        _ = await SaveFile(
            folder,
            predictionFile,
            FileUtils.GetServerFileName(fileNamePrefix, predictionFileExtension, true));

        return RedirectToAction(
            "RunPredict",
            new {
                task_id = taskId, prefix = fileNamePrefix, delimiter = separator, index = Convert.ToInt32(!hasIndex), algorithm = algorithms.First()}
            );
    }

    public Task<IActionResult> RunPredict(
        [FromQuery(Name = "task_id")] string taskId, 
        string prefix, 
        string delimiter, 
        int index, 
        string algorithm)
    {
        var userId = Convert.ToInt32(_httpContextAccessor.GetClaimValue(ClaimTypes.Sid));
        var fileName = FileUtils.FindSessionFiles(_environment.WebRootPath, userId, prefix).Last().FullName;
        
        var uri = Requests.GetRequestUri(_apiHttpClient?.BaseAddress, ApiEndpoints.Predict);
        var request = new PredictionRequest
        {
            TaskId = taskId,
            FileName = fileName,
            Separator = delimiter,
            Index = index,
            Algorithms = new[] {algorithm}
        };

        _apiHttpClient?.SendAsync(Requests.GetRequestMessage(request, uri)).ConfigureAwait(false);

        return Task.FromResult<IActionResult>(
            RedirectToAction("ViewPredictions", "Model", new {task_id = taskId}));
    }

    public async Task<IActionResult> RunTrain(string prefix, string delimiter, int index, IFormCollection data)
    {
        var target = data.GetValue(TargetColumnName);
        var problemType = data.GetValue(ProblemType);
        var algorithms = data.GetValuesByPrefix(AlgorithmPrefix);
        
        var userId = Convert.ToInt32(_httpContextAccessor.GetClaimValue(ClaimTypes.Sid));
        var files = FileUtils.FindSessionFiles(_environment.WebRootPath, userId, prefix);

        var runRequestUri = Requests.GetRequestUri(_apiHttpClient?.BaseAddress, ApiEndpoints.RunTask);
        var taskRequest = new RunTaskRequest {UserId = userId};
        
        var response = await _apiHttpClient?.SendAsync(Requests.GetRequestMessage(taskRequest, runRequestUri))!;
        var taskId = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result)["taskId"]
            .ToString();

        var trainingFileName = files.First().FullName;

        var trainRequestUri = Requests.GetRequestUri(_apiHttpClient?.BaseAddress, ApiEndpoints.Train);
        var trainRequest = new TrainRequest
        {
            TaskId = taskId,
            FileName = trainingFileName,
            Separator = delimiter,
            ProblemType = problemType,
            Target = target,
            Index = index,
            Algorithms = algorithms
        };
        
        _apiHttpClient?.SendAsync(Requests.GetRequestMessage(trainRequest, trainRequestUri)).ConfigureAwait(false);

        return RedirectToAction("Log", "Task", new {task_id = taskId});
    }

    public async Task<IActionResult> SelectModels([FromQuery(Name = "task_id")] string taskId)
    {
        var userId = Convert.ToInt32(_httpContextAccessor.GetClaimValue(ClaimTypes.Sid));
        var commonPath = Path.Combine(FileUtils.GetUserFolder(_environment.WebRootPath, userId), taskId);
        
        var metricsPath = Path.Combine(commonPath, "metrics.csv");
        var files = new DirectoryInfo(commonPath).GetFiles();
        var algorithms = (from file in files 
                where file.Name.EndsWith(".pickle") 
                select file.Name.Replace(".pickle", string.Empty)
                    .ToCamelCase())
            .ToHashSet();

        var metrics = await System.IO.File.ReadAllLinesAsync(metricsPath);
        var metricsToShow = (from metric in metrics 
            let algorithm = metric.Split(",")[0] 
            where algorithms.Contains(algorithm) 
            select metric)
            .ToList();

        metricsToShow.Insert(0,
            metrics[0].Split(",").Length == 4
                ? "Model, Score, MSE, MAE"
                : "Model, Score, Accuracy, Precision, Recall, F1, ROC-AUC");
        ViewBag.Metrics = metricsToShow;

        return View();
    }
    
    public IActionResult SaveSelectedModels([FromQuery(Name = "task_id")] string taskId, IFormCollection data)
    {
        var userId = Convert.ToInt32(_httpContextAccessor.GetClaimValue(ClaimTypes.Sid));
        var algorithms = data.GetValuesByPrefix(AlgorithmPrefix);
        var algorithmsToSave = new HashSet<string>();
        foreach (var algorithm in algorithms)
            algorithmsToSave.Add(algorithm.RemovePrefix(AlgorithmPrefix).ToSnakeCase().AddSuffix(".pickle"));

        var path = Path.Combine(FileUtils.GetUserFolder(_environment.WebRootPath, userId), taskId);
        var files = new DirectoryInfo(path).GetFiles();
        foreach (var file in files)
        {
            if (file.Name.EndsWith(".pickle") && !algorithmsToSave.Contains(file.Name))
            {
                file.Delete();
            }
        }

        return RedirectToAction(
            "Index", "Account", 
            new{user_id = Convert.ToInt32(_httpContextAccessor.GetClaimValue(ClaimTypes.Sid))});
    }
    
    public IActionResult DeleteModel([FromQuery(Name = "task_id")] string taskId, [FromQuery] string algorithm)
    {
        var userId = Convert.ToInt32(_httpContextAccessor.GetClaimValue(ClaimTypes.Sid));
        var algorithmToDelete = algorithm.RemovePrefix(AlgorithmPrefix).ToSnakeCase().AddSuffix(".pickle");
        var path = Path.Combine(FileUtils.GetUserFolder(_environment.WebRootPath, userId), taskId, algorithmToDelete);
        System.IO.File.Delete(path);

        return RedirectToAction(
            "Index", "Account", 
            new{user_id = Convert.ToInt32(_httpContextAccessor.GetClaimValue(ClaimTypes.Sid))});
    }
    
    [Route("model/predictions")]
    public Task<IActionResult> ViewPredictions([FromQuery(Name = "task_id")] string taskId)
    {
        var userId = Convert.ToInt32(_httpContextAccessor.GetClaimValue(ClaimTypes.Sid));
        var path = Path.Combine(FileUtils.GetUserFolder(_environment.WebRootPath, userId), taskId);
        var filesWithPredictions = new DirectoryInfo(path);
        var filesToShow = 
            (from file in filesWithPredictions.GetFiles() 
                where file.Name.EndsWith("_predictions.csv") 
                select file.Name.Replace("_predictions.csv", string.Empty))
            .ToList();

        ViewBag.Predictions = filesToShow;

        return Task.FromResult<IActionResult>(View());
    }

    [Route("model/download_predictions")]
    public Task<IActionResult> DownloadPredictions([FromQuery(Name = "task_id")] string taskId, [FromQuery(Name="algorithm")] string algorithm)
    {
        var userId = Convert.ToInt32(_httpContextAccessor.GetClaimValue(ClaimTypes.Sid));
        var path = Path.Combine(FileUtils.GetUserFolder(_environment.WebRootPath, userId), taskId, $"{algorithm}_predictions.csv");
        var bytes = System.IO.File.ReadAllBytes(path);
        var memory = new MemoryStream(bytes);
        memory.Position = 0;

        return Task.FromResult<IActionResult>(
            File(memory.ToArray(), "text/plain", $"{algorithm}_predictions.csv")
        );
    }
    
    [HttpGet("read_data")]
    public List<string> ReadData([FromQuery(Name = "task_id")] string taskId)
    {
        var userId = Convert.ToInt32(_httpContextAccessor.GetClaimValue(ClaimTypes.Sid));
        var path = Path.Combine(FileUtils.GetUserFolder(_environment.WebRootPath, userId), taskId);
        var filesWithPredictions = new DirectoryInfo(path);
        var filesToShow = 
            (from file in filesWithPredictions.GetFiles() 
                where file.Name.EndsWith("_predictions.csv") 
                select file.Name.Replace("_predictions.csv", string.Empty))
            .ToList();
        
        return filesToShow;
    }

    private static async Task<string> SaveFile(string folder, IFormFile file, string fileName)
    {
        long size = 0;
        var filepath = Path.Combine(folder, fileName);
        if (file is {Length: > 0})
        {
            await using var stream = new FileStream(filepath, FileMode.Create);
            await file.CopyToAsync(stream);
            size = file.Length;
        }

        return size > 0 ? $"{fileName} ({size} bytes)" : null;
    }

    #region AlgorithmsHTMLRendering

    private void GetAlgorithmsHtml()
    {
        const int methodsSelectedCount = 0;
        var regressionAlgorithmsHtml = GetAlgorithmsHtml(RegressionAlgorithms);
        var classificationAlgorithmsHtml = GetAlgorithmsHtml(ClassificationAlgorithms);

        ViewBag.MethodsSelectedCount = methodsSelectedCount.ToString();
        ViewBag.ClassificationMethods = classificationAlgorithmsHtml;
        ViewBag.RegressionMethods = regressionAlgorithmsHtml;
    }

    private static string GetAlgorithmsHtml(Dictionary<string, string> algorithms)
    {
        var stringBuilder = new StringBuilder();

        foreach (var (name, settings) in algorithms)
        {
            stringBuilder.AppendLine(
                $@"<div class=""input-group mb-3"">
                      <div class=""input-group-prepend"">
                        <div class=""input-group-text"">
                          <input type=""checkbox"" value=""1"" name=""alg_{name}"" aria-label=""{name}"" class=""mr-2""> {name}
                        </div>
                      </div>
                      <input type=""text"" class=""form-control"" aria-label=""{name}"" value='{settings}'>
                    </div>"
            );
        }

        return stringBuilder.ToString();
    }

    private Dictionary<string, string> _regressionAlgorithms;

    private Dictionary<string, string> _classificationAlgorithms;

    private Dictionary<string, string> RegressionAlgorithms =>
        _regressionAlgorithms ??=
            FileUtils.LoadAlgorithmsFromJsonFile(_environment.WebRootPath, "py/!algorithms.regression.json");

    private Dictionary<string, string> ClassificationAlgorithms =>
        _classificationAlgorithms ??=
            FileUtils.LoadAlgorithmsFromJsonFile(_environment.WebRootPath, "py/!algorithms.classification.json");

    #endregion

    #region FeatureNamesRenderingHTML

    private static string[] ExtractFeatureNamesFromExcel(string fileName, int index) => 
        FileUtils.GetHeaderFromExcelFile(fileName, index);

    private static string[] ExtractFeatureNamesFromCsv(string fileName, char delimiter, int index) => 
        FileUtils.GetHeaderFromCsvFile(fileName, delimiter, index);

    #endregion

    #region Alerts

    private string GetErrorAlertHTML(string message) =>
        $"<div class=\"alert alert-danger montserrat-font\" role=\"alert\">{message}</div>";

    #endregion

    #region HTMLElementsNames

    private const string TargetColumnName = "target_column";
    private const string ProblemType = "problem_type";
    private const string Separator = "separator";
    private const string Prefix = "prefix";
    private const string NoIndex = "hasNoIndex";
    private const string Off = "off";

    #endregion
    
    #region Constants

    private const string AlgorithmPrefix = "alg_";
    private const string ErrorMessage = "ErrorMessage";

    #endregion
}