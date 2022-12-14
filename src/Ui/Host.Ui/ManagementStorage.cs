using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CrossBusExplorer.Management;
using CrossBusExplorer.Management.Contracts;
namespace CrossBusExplorer.Host.Ui;

public class ManagementStorage : IManagementStorage
{
    private const string ServiceBusConnectionsFileName = "servicebusconnections.json";
    
    public async Task StoreAsync(
        IDictionary<string, ServiceBusConnection> connections,
        CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(
            FilePath,
            JsonSerializer.Serialize(connections),
            Encoding.UTF8,
            cancellationToken);
    }
    
    public async Task<IDictionary<string, ServiceBusConnection>> ReadAsync(
        CancellationToken cancellationToken)
    {
        if (File.Exists(FilePath))
        {
            var serializedData = await File.ReadAllTextAsync(FilePath, cancellationToken);

            return JsonSerializer.Deserialize<IDictionary<string, ServiceBusConnection>>(
                serializedData) ?? new Dictionary<string, ServiceBusConnection>();
        }

        return new Dictionary<string, ServiceBusConnection>();
    }

    private string FilePath => Path.Combine(
        FileSystem.AppDataDirectory,
        ServiceBusConnectionsFileName);
}