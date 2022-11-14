using System;
using CrossBusExplorer.Website.Exceptions;
using MudBlazor;
namespace CrossBusExplorer.Website;

internal static class WellKnown
{
    internal static int ProgressCompleted = 100;
    internal static string DefaultConfirmSuccessResult => "confirm";

    internal static class Regex
    {
        public static string TimeSpan = @"\d+\.((0?\d)|(1\d)|(2[0-3]))(:[0-5]\d){2}";
    }

    internal static class Format
    {
        public static string DateFormat = "dd.MM.yyyy hh:MM:ss";
    }

    public static class Converters
    {
        public static Converter<TimeSpan?> TimeSpanConverter = new Converter<TimeSpan?>
        {
            SetFunc = value => value != null
                ? value.Value.ToString(@"dddd\.hh\:mm\:ss")
                : "0000.00:00:00",
            GetFunc = text =>
            {
                if (TimeSpan.TryParse(text, out TimeSpan value))
                {
                    return value;
                }

                return null;
            }
        };

        public static Converter<DateTimeOffset?> DateTimeOffsetConverter =
            new Converter<DateTimeOffset?>
            {
                SetFunc = value => value != null
                    ? value.Value.ToString(Format.DateFormat)
                    : DateTimeOffset.MinValue.ToString(Format.DateFormat),
                GetFunc = text =>
                {
                    if (DateTimeOffset.TryParse(text, out DateTimeOffset value))
                    {
                        return value;
                    }

                    return null;
                }
            };
    }
}