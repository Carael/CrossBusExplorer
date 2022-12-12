using System.Text;
using CrossBusExplorer.Management;
using ElectronNET.API;
using ElectronNET.API.Entities;
namespace Website.Host;

public class ManagementStorageElectron : IManagementStorage
{
    private const string ServiceBusConnectionsFileName = "cross_bus_explorer_connections.json";

    public async Task StoreAsync(string content, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(
            await FilePath(cancellationToken),
            content,
            Encoding.UTF8,
            cancellationToken);
    }
    
    public async Task<string?> ReadAsync(CancellationToken cancellationToken)
    {
        var path = await FilePath(cancellationToken);
        
        if (File.Exists(Path.Combine(path)))
        {
            return await File.ReadAllTextAsync(path, cancellationToken);
        }

        return null;
    }

    private async Task<string> FilePath(CancellationToken cancellationToken)
    {
        return Path.Combine(
            await Electron.App.GetPathAsync(PathName.UserData, cancellationToken),
            ServiceBusConnectionsFileName);
    }  
}