using System;
using CrossBusExplorer.Website.Exceptions;
using MudBlazor;
namespace CrossBusExplorer.Website;

internal static class WellKnown
{
    internal static string DefaultConfirmSuccessResult => "confirm";

    internal static class Regex
    {
        public static string TimeSpan = @"\d+\.((0?\d)|(1\d)|(2[0-3]))(:[0-5]\d){2}";
    }

    internal static class Mask
    {
        public static string TimeSpanHelperText = "Format: DDDD.HH:MM:SS";
    }

    internal static class Converters
    {
        public static Converter<TimeSpan?> TimeSpanConverter = new Converter<TimeSpan?>
        {
            SetFunc = value => value != null 
                ? value.Value.ToString(@"dddd\.hh\:mm\:ss") 
                : "0000.00:00:00",
            GetFunc = text =>
            {
                if(TimeSpan.TryParse(text, out TimeSpan value))
                {
                    return value;
                }

                return null;
            }
        };
    }
}