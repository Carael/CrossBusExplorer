namespace CrossBusExplorer.ServiceBus;

public static class StringExtensions
{
    public static string? RemoveUrl(this string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var uri = new Uri(value);

        return uri.LocalPath.Remove(0,1);
    }
}