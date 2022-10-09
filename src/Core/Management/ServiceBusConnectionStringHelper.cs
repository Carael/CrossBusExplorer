using Azure.Messaging.ServiceBus;
namespace CrossBusExplorer.Management;

public static class ServiceBusConnectionStringHelper
{
    public static string GetNameFromConnectionString(string connectionString)
    {
        var connectionStringProperties =
            ServiceBusConnectionStringProperties.Parse(connectionString);

        return connectionStringProperties.EntityPath;
    }

    public static bool IsValid(string? connectionString)
    {
        try
        {
            ServiceBusConnectionStringProperties.Parse(connectionString);

            return true;
        }
        catch
        {
            return false;
        }
    }
}