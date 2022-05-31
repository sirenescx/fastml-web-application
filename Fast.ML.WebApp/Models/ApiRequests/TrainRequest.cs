using Newtonsoft.Json;

namespace Fast.ML.WebApp.Models.ApiRequests;

public class TrainRequest
{
    [JsonProperty("taskId")]
    public string TaskId { get; set; }
    
    [JsonProperty("filename")]
    public string FileName { get; set; }
    
    [JsonProperty("separator")]
    public string Separator { get; set; }
    
    [JsonProperty("problemType")]
    public string ProblemType { get; set; }
    
    [JsonProperty("target")]
    public string Target { get; set; }
    
    [JsonProperty("index")]
    public int Index { get; set; }
    
    [JsonProperty("algorithms")]
    public string[] Algorithms { get; set; }
}