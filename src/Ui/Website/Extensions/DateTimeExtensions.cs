using System;
namespace CrossBusExplorer.Website.Extensions;

public static class DateTimeExtensions
{
    public static string ToUniversal(this DateTimeOffset? value)
    {
        if (value != null && value != DateTimeOffset.MinValue && value != DateTimeOffset.MaxValue)
        {
            return value.Value.ToString("u");
        }

        return "-";
    }

    public static string ToUniversal(this DateTimeOffset value)
    {
        if (value != DateTimeOffset.MinValue && value != DateTimeOffset.MaxValue)
        {
            return value.ToString("u");
        }

        return "-";
    }
}