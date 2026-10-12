namespace CrossBusExplorer.Management.Contracts;

public class ServiceBusConnection
{
    public ServiceBusConnection(
        string name,
        string? connectionString,
        Uri endpoint,
        string fullyQualifiedName,
        string? entityPath,
        string? sharedAccessKey,
        string? sharedAccessSignature,
        string? sharedAccessKeyName,
        ServiceBusTransportType transportType = ServiceBusTransportType.AmqpTcp,
        ServiceBusAuthenticationType authenticationType = ServiceBusAuthenticationType.ConnectionString,
        string? tenantId = null,
        string? clientId = null,
        string? tokenFilePath = null,
        string? folder = null)
    {
        Name = name;
        ConnectionString = connectionString;
        Endpoint = endpoint;
        FullyQualifiedName = fullyQualifiedName;
        EntityPath = entityPath;
        SharedAccessKey = sharedAccessKey;
        SharedAccessSignature = sharedAccessSignature;
        SharedAccessKeyName = sharedAccessKeyName;
        TransportType = transportType;
        AuthenticationType = authenticationType;
        TenantId = tenantId;
        ClientId = clientId;
        TokenFilePath = tokenFilePath;
        Folder = folder?.Trim() ?? string.Empty;
    }

    public static ServiceBusConnection CreatePasswordless(
        string name,
        string fullyQualifiedName,
        ServiceBusAuthenticationType authenticationType,
        ServiceBusTransportType transportType = ServiceBusTransportType.AmqpTcp,
        string? tenantId = null,
        string? clientId = null,
        string? tokenFilePath = null,
        string? folder = null)
    {
        if (authenticationType == ServiceBusAuthenticationType.ConnectionString)
        {
            throw new ArgumentException(
                "A connection string is required for connection-string authentication.",
                nameof(authenticationType));
        }

        var normalizedNamespace = fullyQualifiedName
            .Replace("sb://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim()
            .TrimEnd('/');

        return new ServiceBusConnection(
            name,
            null,
            new Uri($"sb://{normalizedNamespace}"),
            normalizedNamespace,
            null,
            null,
            null,
            null,
            transportType,
            authenticationType,
            tenantId,
            clientId,
            tokenFilePath,
            folder);
    }

    public string Name { get; }
    public string? ConnectionString { get; }
    public Uri Endpoint { get; }
    public string FullyQualifiedName { get; }
    public string? EntityPath { get; }
    public string? SharedAccessKey { get; }
    public string? SharedAccessSignature { get; }
    public string? SharedAccessKeyName { get; }
    public ServiceBusTransportType TransportType { get; }
    public ServiceBusAuthenticationType AuthenticationType { get; }
    public string? TenantId { get; }
    public string? ClientId { get; }
    public string? TokenFilePath { get; }
    public string Folder { get; }
}
