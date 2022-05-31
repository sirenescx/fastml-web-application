using Newtonsoft.Json;

namespace Fast.ML.WebApp.Models.ApiRequests;

public class RunTaskRequest
{
    [JsonProperty("userId")]
    public int UserId { get; set; }
}