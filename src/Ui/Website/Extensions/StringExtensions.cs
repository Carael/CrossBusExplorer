using System;
using System.Linq;
using Newtonsoft.Json;
namespace CrossBusExplorer.Website.Extensions;

public static class StringExtensions
{
    public static bool EqualsInvariantIgnoreCase(this string? value, string value2)
    {
        if (value == null && value2 == null)
        {
            return true;
        }

        if (value == null && value2 != null)
        {
            return false;
        }

        return value.Equals(value2, StringComparison.InvariantCultureIgnoreCase);
    }


    public static TimeSpan? ToTimeSpan(this string? value)
    {
        return value != null ? TimeSpan.Parse(value) : null;
    }

    public static string TryFormatBody(this string value, string? contentType)
    {
        if (contentType != null)
        {
            return contentType.Contains("json", StringComparison.InvariantCultureIgnoreCase) 
                ? TryFormatJson(value) 
                : value;
        }

        return TryFormatJson(value);
    }

    public static string SplitAndGetLastSection(this string value, char split)
    {
        if (value.Contains(split))
        {
            return value.Split(split).Last();
        }

        return value;
    }

    private static string TryFormatJson(string value)
    {
        try
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(value);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }
        catch
        {
            return value;
        }
    }
}
