using System;
namespace CrossBusExplorer.Website.Extensions;

public static class StringExtensions
{
    public static bool EqualsInvariantIgnoreCase(this string value, string value2) =>
        value.Equals(value2, StringComparison.InvariantCultureIgnoreCase);
}