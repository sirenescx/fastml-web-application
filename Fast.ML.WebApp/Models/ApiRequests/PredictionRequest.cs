using Newtonsoft.Json;

namespace Fast.ML.WebApp.Models.ApiRequests;

public class PredictionRequest
{
    [JsonProperty("taskId")]
    public string TaskId { get; set; }
    
    [JsonProperty("filename")]
    public string FileName { get; set; }
    
    [JsonProperty("separator")]
    public string Separator { get; set; }
    
    [JsonProperty("index")]
    public int Index { get; set; }
    
    [JsonProperty("algorithms")]
    public string[] Algorithms { get; set; }
}