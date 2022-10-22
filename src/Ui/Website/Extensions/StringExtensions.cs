using System;
using Newtonsoft.Json;
namespace CrossBusExplorer.Website.Extensions;

public static class StringExtensions
{
    public static bool EqualsInvariantIgnoreCase(this string value, string value2) =>
        value.Equals(value2, StringComparison.InvariantCultureIgnoreCase);

    public static TimeSpan? ToTimeSpan(this string? value)
    {
        return value != null ? TimeSpan.Parse(value) : null;
    }

    public static string TryFormatBody(this string value, string contentType)
    {
        if (contentType != null)
        {
            if (contentType.Contains("json", StringComparison.InvariantCultureIgnoreCase))
            {
                return TryFormatJson(value);
            }
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