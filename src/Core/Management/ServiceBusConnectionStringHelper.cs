using Azure.Messaging.ServiceBus;
using CrossBusExplorer.Management.Contracts;
namespace CrossBusExplorer.Management;

public static class ServiceBusConnectionStringHelper
{
    public static string? TryGetNameFromConnectionString(string connectionString)
    {
        try
        {
            var connectionStringProperties =
                ServiceBusConnectionStringProperties.Parse(connectionString);

            if (connectionStringProperties.FullyQualifiedNamespace.IndexOf('.') != -1)
            {
                return connectionStringProperties.FullyQualifiedNamespace.Split('.')[0];
            }

            return connectionStringProperties.FullyQualifiedNamespace;
        }
        catch
        {
            return null;
        }
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

    public static ServiceBusConnection GetServiceBusConnection(
        string name,
        string connectionString)
    {
        var properties =
            ServiceBusConnectionStringProperties.Parse(connectionString);

        return new ServiceBusConnection(
            name,
            ServiceBusConnectionType.ConnectionString,
            connectionString,
            properties.Endpoint,
            properties.FullyQualifiedNamespace,
            properties.EntityPath,
            properties.SharedAccessKey,
            properties.SharedAccessSignature,
            properties.SharedAccessKeyName);
    }
}
