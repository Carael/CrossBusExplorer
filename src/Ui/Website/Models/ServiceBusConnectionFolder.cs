using System;
using CrossBusExplorer.Management.Contracts;
namespace CrossBusExplorer.Website.Models;

public class ServiceBusConnectionWithFolder : ServiceBusConnection
{
    public string Folder { get; private set; }

    public void UpdateFolder(string folder)
    {
        Folder = folder;
    }

    public ServiceBusConnectionWithFolder(ServiceBusConnection serviceBusConnection, string folder)
        : base(
            serviceBusConnection.Name,
            serviceBusConnection.Type,
            serviceBusConnection.ConnectionString,
            serviceBusConnection.Endpoint,
            serviceBusConnection.FullyQualifiedName,
            serviceBusConnection.EntityPath,
            serviceBusConnection.SharedAccessKey,
            serviceBusConnection.SharedAccessSignature,
            serviceBusConnection.SharedAccessKeyName)
    {
        Folder = folder;
    }
}
