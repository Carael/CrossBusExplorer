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

    public static string TryFormatJson(this string value, string contentType)
    {
        if (contentType != null &&
            !contentType.Contains("json", StringComparison.InvariantCultureIgnoreCase))
        {
            return contentType;
        }

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