using CrossBusExplorer.Management.Contracts;
namespace CrossBusExplorer.Website.Models;

public class ServiceBusConnectionWithFolder
{
    public ServiceBusConnectionWithFolder(
        ServiceBusConnection serviceBusConnection,
        string folder)
    {
        ServiceBusConnection = serviceBusConnection;
        Folder = folder;
    }
    
    public ServiceBusConnection ServiceBusConnection { get; }
    public string Folder { get; private set; }

    public void UpdateFolder(string folder)
    {
        Folder = folder;
    }
}