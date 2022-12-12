using System.Text;
using System.Text.Json;
using CrossBusExplorer.Management;
using CrossBusExplorer.Management.Contracts;
using ElectronNET.API;
using ElectronNET.API.Entities;
namespace Website.Host;

public class ManagementStorageElectron : IManagementStorage
{
    private const string ServiceBusConnectionsFileName = "cross_bus_explorer_connections.json";

    public async Task StoreAsync(
        IDictionary<string, ServiceBusConnection> connections,
        CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(
            await FilePath(cancellationToken),
            JsonSerializer.Serialize(connections),
            Encoding.UTF8,
            cancellationToken);
    }
    
    public async Task<IDictionary<string, ServiceBusConnection>> ReadAsync(
        CancellationToken cancellationToken)
    {
        var path = await FilePath(cancellationToken);
        
        if (File.Exists(Path.Combine(path)))
        {
            var serializedData = await File.ReadAllTextAsync(path, cancellationToken);

            return JsonSerializer.Deserialize<IDictionary<string, ServiceBusConnection>>(
                serializedData, new JsonSerializerOptions
                {
                    
                }) ?? new Dictionary<string, ServiceBusConnection>();
        }

        return new Dictionary<string, ServiceBusConnection>();
    }

    private async Task<string> FilePath(CancellationToken cancellationToken)
    {
        return Path.Combine(
            await Electron.App.GetPathAsync(PathName.UserData, cancellationToken),
            ServiceBusConnectionsFileName);
    }  
}