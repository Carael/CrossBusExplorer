using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using CrossBusExplorer.Management;
using CrossBusExplorer.Management.Contracts;
using Path = System.IO.Path;

namespace CrossBusExplorer.Host;

public sealed class DesktopManagementStorage : IManagementStorage
{
    private const string ConnectionsFileName = "connections.v2.json";
    private const string SecretsFileName = "connection-secrets.v1.json";
    private const string PreviousConnectionsFileName = "connections.v1.json";
    private const string LegacyConnectionsFileName = "cross_bus_explorer_connections.json";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _dataDirectory;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public DesktopManagementStorage()
    {
        _dataDirectory = Environment.GetEnvironmentVariable("CROSSBUS_DATA_DIR")
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CrossBusExplorer");

        Directory.CreateDirectory(_dataDirectory);
    }

    public async Task StoreAsync(
        IDictionary<string, ServiceBusConnection> connections,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await StoreCoreAsync(connections, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IDictionary<string, ServiceBusConnection>> ReadAsync(
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var currentPath = Path.Combine(_dataDirectory, ConnectionsFileName);
            if (File.Exists(currentPath))
            {
                return await ReadCurrentAsync(currentPath, cancellationToken);
            }

            var legacyPath = ResolveLegacyPath();
            if (legacyPath is null)
            {
                return NewConnectionDictionary();
            }

            var legacyJson = await File.ReadAllTextAsync(legacyPath, cancellationToken);
            var legacyConnections = JsonSerializer.Deserialize<Dictionary<string, ServiceBusConnection>>(
                                        legacyJson,
                                        _serializerOptions)
                                    ?? NewConnectionDictionary();

            await StoreCoreAsync(legacyConnections, cancellationToken);
            return new Dictionary<string, ServiceBusConnection>(
                legacyConnections,
                StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IDictionary<string, ServiceBusConnection>> ReadCurrentAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var document = JsonSerializer.Deserialize<ConnectionStoreDocument>(
                           await File.ReadAllTextAsync(path, cancellationToken),
                           _serializerOptions)
                       ?? new ConnectionStoreDocument(2, []);
        var secrets = await ReadSecretsAsync(cancellationToken);
        var connections = NewConnectionDictionary();

        foreach (var (name, profile) in document.Connections)
        {
            secrets.TryGetValue(profile.SecretReference ?? name, out var connectionString);
            connections[name] = profile.ToConnection(connectionString);
        }

        return connections;
    }

    private async Task StoreCoreAsync(
        IDictionary<string, ServiceBusConnection> connections,
        CancellationToken cancellationToken)
    {
        var profiles = new Dictionary<string, StoredConnectionProfile>(
            StringComparer.OrdinalIgnoreCase);
        var secrets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, connection) in connections)
        {
            profiles[name] = StoredConnectionProfile.From(connection);
            if (!string.IsNullOrWhiteSpace(connection.ConnectionString))
            {
                secrets[name] = connection.ConnectionString;
            }
        }

        await WriteAtomicAsync(
            Path.Combine(_dataDirectory, ConnectionsFileName),
            JsonSerializer.Serialize(new ConnectionStoreDocument(2, profiles), _serializerOptions),
            cancellationToken);
        await WriteAtomicAsync(
            Path.Combine(_dataDirectory, SecretsFileName),
            JsonSerializer.Serialize(secrets, _serializerOptions),
            cancellationToken);
    }

    private async Task<Dictionary<string, string>> ReadSecretsAsync(
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(_dataDirectory, SecretsFileName);
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(
                         await File.ReadAllTextAsync(path, cancellationToken),
                         _serializerOptions)
                     ?? [];
        return new Dictionary<string, string>(result, StringComparer.OrdinalIgnoreCase);
    }

    private async Task WriteAtomicAsync(
        string path,
        string contents,
        CancellationToken cancellationToken)
    {
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllTextAsync(temporaryPath, contents, cancellationToken);
        RestrictFilePermissions(temporaryPath);
        File.Move(temporaryPath, path, true);
    }

    private string? ResolveLegacyPath()
    {
        var explicitLegacyPath = Environment.GetEnvironmentVariable(
            "CROSSBUS_LEGACY_CONNECTIONS_FILE");
        var legacyCandidates = new[]
        {
            explicitLegacyPath,
            Path.Combine(_dataDirectory, PreviousConnectionsFileName),
            Path.Combine(_dataDirectory, LegacyConnectionsFileName),
            Path.Combine(Directory.GetCurrentDirectory(), LegacyConnectionsFileName)
        };

        return legacyCandidates.FirstOrDefault(
            path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
    }

    private static Dictionary<string, ServiceBusConnection> NewConnectionDictionary() =>
        new(StringComparer.OrdinalIgnoreCase);

    private static void RestrictFilePermissions(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    private sealed record ConnectionStoreDocument(
        int Version,
        Dictionary<string, StoredConnectionProfile> Connections);

    private sealed record StoredConnectionProfile(
        string Name,
        string FullyQualifiedName,
        string? EntityPath,
        ServiceBusTransportType TransportType,
        ServiceBusAuthenticationType AuthenticationType,
        string? TenantId,
        string? ClientId,
        string? TokenFilePath,
        string? Folder,
        string? SecretReference)
    {
        public static StoredConnectionProfile From(ServiceBusConnection connection) =>
            new(
                connection.Name,
                connection.FullyQualifiedName,
                connection.EntityPath,
                connection.TransportType,
                connection.AuthenticationType,
                connection.TenantId,
                connection.ClientId,
                connection.TokenFilePath,
                connection.Folder,
                connection.AuthenticationType == ServiceBusAuthenticationType.ConnectionString
                    ? connection.Name
                    : null);

        public ServiceBusConnection ToConnection(string? connectionString)
        {
            if (AuthenticationType == ServiceBusAuthenticationType.ConnectionString)
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        $"The stored secret for connection '{Name}' is missing.");
                }

                return ServiceBusConnectionStringHelper.GetServiceBusConnection(
                    Name,
                    connectionString,
                    TransportType,
                    Folder);
            }

            return ServiceBusConnection.CreatePasswordless(
                Name,
                FullyQualifiedName,
                AuthenticationType,
                TransportType,
                TenantId,
                ClientId,
                TokenFilePath,
                Folder);
        }
    }
}
