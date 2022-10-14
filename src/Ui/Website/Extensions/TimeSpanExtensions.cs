using System;
namespace CrossBusExplorer.Website.Extensions;

public static class TimeSpanExtensions
{
    public static string ToTimeSpanString(this TimeSpan timeSpan)
        => timeSpan.ToString(@"ddd\.hh\:mm\:ss");
}