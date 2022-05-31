using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Fast.ML.WebApp.Utils;

public class Requests
{
    private const string ContentType = "application/json";

    public static HttpRequestMessage GetRequestMessage(object request, Uri uri)
    {
        var content = JsonConvert.SerializeObject(request);
        var httpContent = new StringContent(content, Encoding.UTF8, ContentType);
        return new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = httpContent
        };
    }

    public static Uri GetRequestUri(Uri baseAddress, string endpoint) =>
        new(baseAddress ?? new Uri(string.Empty), endpoint);
}