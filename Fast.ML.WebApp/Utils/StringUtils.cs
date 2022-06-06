using System;
using System.Text;

namespace Fast.ML.WebApp.Utils;

public static class StringUtils
{
    public static bool IsChar(this string text) => 
        !string.IsNullOrEmpty(text) && text.Length == 1;

    public static string RemovePrefix(this string text, string prefix) => 
        text.Replace(prefix, string.Empty);
    
    public static string AddSuffix(this string text, string suffix) => 
        string.Concat(text, suffix);

    public static string ToSnakeCase(this string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (text.Length < 2)
        {
            return text;
        }

        var sb = new StringBuilder();
        sb.Append(char.ToLowerInvariant(text[0]));
        for (var i = 1; i < text.Length; ++i)
        {
            var c = text[i];
            if (char.IsUpper(c))
            {
                sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
    
    public static string ToCamelCase(this string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var parts = text.Split("_");
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            sb.Append(char.ToUpper(part[0]) + part[1..]);
        }

        return sb.ToString();
    }
}