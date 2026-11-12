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
            new Dictionary<string, ServiceBusConnection> { [connection.Id] = connection },
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
        Assert.Equal(connection.Id, restored.Key);
        Assert.Equal(connection.Id, restored.Value.Id);
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
        var cli = ServiceBusConnection.CreatePasswordless(
            "cli",
            "cli.servicebus.windows.net",
            ServiceBusAuthenticationType.AzureCli,
            tenantId: "tenant",
            folder: "Development",
            id: "cli-id");
        var workload = ServiceBusConnection.CreatePasswordless(
            "workload",
            "workload.servicebus.windows.net",
            ServiceBusAuthenticationType.WorkloadIdentity,
            tenantId: "tenant",
            clientId: "client",
            tokenFilePath: "/var/run/secrets/azure/token",
            id: "workload-id");
        var profiles = new Dictionary<string, ServiceBusConnection>
        {
            [cli.Id] = cli,
            [workload.Id] = workload
        };

        await storage.StoreAsync(profiles, CancellationToken.None);
        var restored = await storage.ReadAsync(CancellationToken.None);

        Assert.Equal(ServiceBusAuthenticationType.AzureCli, restored[cli.Id].AuthenticationType);
        Assert.Equal("tenant", restored[cli.Id].TenantId);
        Assert.Equal("Development", restored[cli.Id].Folder);
        Assert.Equal(ServiceBusAuthenticationType.WorkloadIdentity, restored[workload.Id].AuthenticationType);
        Assert.Equal("client", restored[workload.Id].ClientId);
        Assert.Equal("/var/run/secrets/azure/token", restored[workload.Id].TokenFilePath);
        Assert.All(restored.Values, connection => Assert.Null(connection.ConnectionString));
    }

    [Fact]
    public async Task ImportsTheLegacyConnectionFileOnceAndGeneratesAdHocId()
    {
        var legacyPath = Path.Combine(_directory, "cross_bus_explorer_connections.json");
        await File.WriteAllTextAsync(
            legacyPath,
            $$"""
            {
              "legacy": {
                "Name": "legacy",
                "ConnectionString": "{{ConnectionString}}",
                "Endpoint": "sb://payments.servicebus.windows.net/",
                "FullyQualifiedName": "payments.servicebus.windows.net",
                "SharedAccessKeyName": "RootManageSharedAccessKey",
                "SharedAccessKey": "ZmFrZQ==",
                "TransportType": 1,
                "AuthenticationType": 0
              }
            }
            """,
            CancellationToken.None);

        var storage = new DesktopManagementStorage();
        var imported = await storage.ReadAsync(CancellationToken.None);
        var entry = Assert.Single(imported);

        Assert.False(string.IsNullOrWhiteSpace(entry.Value.Id));
        Assert.Equal(entry.Value.Id, entry.Key);
        Assert.Equal("legacy", entry.Value.Name);
        Assert.Equal(ConnectionString, entry.Value.ConnectionString);
        Assert.True(File.Exists(Path.Combine(_directory, "connections.v2.json")));
        Assert.True(File.Exists(Path.Combine(_directory, "connection-secrets.v1.json")));

        var secondRead = await storage.ReadAsync(CancellationToken.None);
        Assert.Equal(entry.Value.Id, Assert.Single(secondRead).Value.Id);
    }

    [Fact]
    public async Task MigratesExistingV2ProfilesWithoutIdAndPersistsStableAdHocId()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_directory, "connections.v2.json"),
            """
            {
              "version": 2,
              "connections": {
                "payments": {
                  "name": "payments",
                  "fullyQualifiedName": "payments.servicebus.windows.net",
                  "authenticationType": 0,
                  "transportType": 1,
                  "folder": "Prod",
                  "secretReference": "payments"
                }
              }
            }
            """,
            CancellationToken.None);
        await File.WriteAllTextAsync(
            Path.Combine(_directory, "connection-secrets.v1.json"),
            $$"""
            {
              "payments": "{{ConnectionString}}"
            }
            """,
            CancellationToken.None);

        var storage = new DesktopManagementStorage();
        var firstRead = await storage.ReadAsync(CancellationToken.None);
        var migrated = Assert.Single(firstRead).Value;

        Assert.False(string.IsNullOrWhiteSpace(migrated.Id));
        Assert.NotEqual("payments", migrated.Id);
        Assert.Equal("payments", migrated.Name);
        Assert.Equal(ConnectionString, migrated.ConnectionString);

        var secondRead = await storage.ReadAsync(CancellationToken.None);
        var secondMigrated = Assert.Single(secondRead).Value;
        Assert.Equal(migrated.Id, secondMigrated.Id);
        Assert.Equal(ConnectionString, secondMigrated.ConnectionString);
    }

    [Fact]
    public async Task AllowsEditingConnectionDisplayNameUsingConnectionId()
    {
        var storage = new DesktopManagementStorage();
        var management = new ConnectionManagement(storage);
        var initial = ServiceBusConnectionStringHelper.GetServiceBusConnection(
            "payments-old",
            ConnectionString,
            folder: "Prod");

        await management.SaveAsync(initial, CancellationToken.None);

        var renamed = ServiceBusConnectionStringHelper.GetServiceBusConnection(
            "payments-new",
            ConnectionString,
            folder: "Prod",
            id: initial.Id);

        await management.SaveAsync(renamed, CancellationToken.None);

        var all = await management.GetAsync(CancellationToken.None);
        var saved = Assert.Single(all);
        Assert.Equal(initial.Id, saved.Id);
        Assert.Equal("payments-new", saved.Name);
        Assert.Equal(ConnectionString, saved.ConnectionString);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("CROSSBUS_DATA_DIR", null);
        Environment.SetEnvironmentVariable("CROSSBUS_LEGACY_CONNECTIONS_FILE", null);
        Directory.Delete(_directory, true);
    }
}
