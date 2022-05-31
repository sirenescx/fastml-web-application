using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Fast.ML.WebApp.Extensions;

public static class IFormCollectionExtensions
{
    public static string GetValue(this IFormCollection formCollection, string key) =>
        formCollection[key].FirstOrDefault();

    public static bool GetValueWithComparison(this IFormCollection formCollection, string key, string valueToCompare) =>
        formCollection[key].Equals(valueToCompare);
    
    public static string[] GetValuesByPrefix(this IFormCollection data, string prefix) =>
        data
            .Where(item => item.Key.StartsWith(prefix))
            .Select(item => item.Key)
            .ToArray();
}