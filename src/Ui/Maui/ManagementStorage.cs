using System.Text;
using CrossBusExplorer.Management;
namespace Maui;

public class ManagementStorage : IManagementStorage
{
    private const string ServiceBusConnectionsFileName = "servicebusconnections.json";
    
    public async Task StoreAsync(string content, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(
            FilePath,
            content,
            Encoding.UTF8,
            cancellationToken);
    }
    public async Task<string?> ReadAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(FilePath))
        {
            return await File.ReadAllTextAsync(FilePath, cancellationToken);
        }

        return null;
    }

    private string FilePath => Path.Combine(
        FileSystem.Current.AppDataDirectory,
        ServiceBusConnectionsFileName);
}