using CrossBusExplorer.Management.Contracts;

namespace CrossBusExplorer.Host.Contracts;

public sealed record SaveConnectionRequest(
    string Name,
    ServiceBusAuthenticationType AuthenticationType,
    string? FullyQualifiedName,
    string? ConnectionString,
    ServiceBusTransportType TransportType,
    string? TenantId,
    string? ClientId,
    string? TokenFilePath,
    string? Folder);

public sealed record ConnectionResponse(
    string Name,
    string FullyQualifiedName,
    ServiceBusAuthenticationType AuthenticationType,
    ServiceBusTransportType TransportType,
    string? TenantId,
    string? ClientId,
    string? TokenFilePath,
    string Folder,
    bool HasStoredSecret)
{
    public static ConnectionResponse From(ServiceBusConnection connection) =>
        new(
            connection.Name,
            connection.FullyQualifiedName,
            connection.AuthenticationType,
            connection.TransportType,
            connection.TenantId,
            connection.ClientId,
            connection.TokenFilePath,
            connection.Folder,
            !string.IsNullOrWhiteSpace(connection.ConnectionString));
}

public sealed record ConnectionTestResponse(
    bool Success,
    string Message,
    TimeSpan Duration);

public sealed record CloneEntityRequest(string Name);
