namespace CrossBusExplorer.Management.Contracts;

public class ServiceBusConnection
{
    public ServiceBusConnection(
        string name,
        string connectionString,
        Uri endpoint,
        string fullyQualifiedName,
        string? entityPath,
        string sharedAccessKey,
        string sharedAccessSignature,
        string sharedAccessKeyName)
    {
        Name = name;
        ConnectionString = connectionString;
        Endpoint = endpoint;
        FullyQualifiedName = fullyQualifiedName;
        EntityPath = entityPath;
        SharedAccessKey = sharedAccessKey;
        SharedAccessSignature = sharedAccessSignature;
        SharedAccessKeyName = sharedAccessKeyName;
    }
    public string Name { get; }
    public string ConnectionString { get; }
    public Uri Endpoint { get; }
    public string FullyQualifiedName { get; }
    public string? EntityPath { get; }
    public string SharedAccessKey { get; }
    public string SharedAccessSignature { get; }
    public string SharedAccessKeyName { get; }
}