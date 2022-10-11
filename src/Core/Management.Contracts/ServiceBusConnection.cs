namespace CrossBusExplorer.Management.Contracts;

public record ServiceBusConnection(
    string Name,
    string ConnectionString,
    Uri Endpoint,
    string FullyQualifiedName,
    string? EntityPath,
    string SharedAccessKey,
    string SharedAccessSignature,
    string SharedAccessKeyName);