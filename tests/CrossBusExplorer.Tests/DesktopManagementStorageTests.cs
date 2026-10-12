using System.Text.Json;
using CrossBusExplorer.Host;
using CrossBusExplorer.Management;
using CrossBusExplorer.Management.Contracts;

namespace CrossBusExplorer.Tests;

public sealed class DesktopManagementStorageTests : IDisposable
{
    private const string ConnectionString =
        "Endpoint=sb://payments.servicebus.windows.net/;" +
        "SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=ZmFrZQ==";
    private readonly string _directory = Directory
        .CreateTempSubdirectory("crossbus-storage-tests-")
        .FullName;

    public DesktopManagementStorageTests()
    {
        Environment.SetEnvironmentVariable("CROSSBUS_DATA_DIR", _directory);
        Environment.SetEnvironmentVariable("CROSSBUS_LEGACY_CONNECTIONS_FILE", null);
    }

    [Fact]
    public async Task StoresSecretsOutsideTheProfileDocument()
    {
        var storage = new DesktopManagementStorage();
        var connection = ServiceBusConnectionStringHelper.GetServiceBusConnection(
            "payments",
            ConnectionString);

        await storage.StoreAsync(
            new Dictionary<string, ServiceBusConnection> { [connection.Name] = connection },
            CancellationToken.None);

        var profiles = await File.ReadAllTextAsync(
            Path.Combine(_directory, "connections.v2.json"),
            CancellationToken.None);
        var secrets = await File.ReadAllTextAsync(
            Path.Combine(_directory, "connection-secrets.v1.json"),
            CancellationToken.None);

        Assert.DoesNotContain("SharedAccessKey", profiles, StringComparison.Ordinal);
        Assert.Contains("SharedAccessKey", secrets, StringComparison.Ordinal);
        var restored = Assert.Single(await storage.ReadAsync(CancellationToken.None));
        Assert.Equal(ConnectionString, restored.Value.ConnectionString);

        if (!OperatingSystem.IsWindows())
        {
            var mode = File.GetUnixFileMode(Path.Combine(_directory, "connection-secrets.v1.json"));
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, mode);
        }
    }

    [Fact]
    public async Task RoundTripsAzureCliAndWorkloadIdentityProfiles()
    {
        var storage = new DesktopManagementStorage();
        var profiles = new Dictionary<string, ServiceBusConnection>
        {
            ["cli"] = ServiceBusConnection.CreatePasswordless(
                "cli",
                "cli.servicebus.windows.net",
                ServiceBusAuthenticationType.AzureCli,
                tenantId: "tenant",
                folder: "Development"),
            ["workload"] = ServiceBusConnection.CreatePasswordless(
                "workload",
                "workload.servicebus.windows.net",
                ServiceBusAuthenticationType.WorkloadIdentity,
                tenantId: "tenant",
                clientId: "client",
                tokenFilePath: "/var/run/secrets/azure/token")
        };

        await storage.StoreAsync(profiles, CancellationToken.None);
        var restored = await storage.ReadAsync(CancellationToken.None);

        Assert.Equal(ServiceBusAuthenticationType.AzureCli, restored["cli"].AuthenticationType);
        Assert.Equal("tenant", restored["cli"].TenantId);
        Assert.Equal("Development", restored["cli"].Folder);
        Assert.Equal(ServiceBusAuthenticationType.WorkloadIdentity, restored["workload"].AuthenticationType);
        Assert.Equal("client", restored["workload"].ClientId);
        Assert.Equal("/var/run/secrets/azure/token", restored["workload"].TokenFilePath);
        Assert.All(restored.Values, connection => Assert.Null(connection.ConnectionString));
    }

    [Fact]
    public async Task ImportsTheLegacyConnectionFileOnce()
    {
        var legacyPath = Path.Combine(_directory, "cross_bus_explorer_connections.json");
        var legacy = ServiceBusConnectionStringHelper.GetServiceBusConnection(
            "legacy",
            ConnectionString);
        await File.WriteAllTextAsync(
            legacyPath,
            JsonSerializer.Serialize(new Dictionary<string, ServiceBusConnection>
            {
                [legacy.Name] = legacy
            }),
            CancellationToken.None);

        var storage = new DesktopManagementStorage();
        var imported = await storage.ReadAsync(CancellationToken.None);

        Assert.Equal(ConnectionString, imported["legacy"].ConnectionString);
        Assert.True(File.Exists(Path.Combine(_directory, "connections.v2.json")));
        Assert.True(File.Exists(Path.Combine(_directory, "connection-secrets.v1.json")));
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("CROSSBUS_DATA_DIR", null);
        Environment.SetEnvironmentVariable("CROSSBUS_LEGACY_CONNECTIONS_FILE", null);
        Directory.Delete(_directory, true);
    }
}
