using System.Collections.Generic;
using System.Linq;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.Website.Extensions;
namespace CrossBusExplorer.Website.Models;

public class FolderSettings
{
    public FolderSettings(
        string name,
        int index,
        List<ServiceBusConnectionSettings>? serviceBusConnectionSettings)
    {
        Name = name;
        Index = index;
        ServiceBusConnectionSettings =
            serviceBusConnectionSettings ?? new List<ServiceBusConnectionSettings>();
    }

    public string Name { get; }
    public int Index { get; private set; }
    public List<ServiceBusConnectionSettings> ServiceBusConnectionSettings { get; }

    public void UpdateIndex(int newIndex)
    {
        Index = newIndex;
    }

    public void AddServiceBusConnectionSetting(
        ServiceBusConnection serviceBusConnection)
    {
        var index = ServiceBusConnectionSettings.MaxBy(p => p.Index)?.Index ?? 0;

        ServiceBusConnectionSettings.Add(
            new ServiceBusConnectionSettings(
                serviceBusConnection.Name,
                index + 1));
    }
    
    public void UpdateServiceBusConnectionsIndexes(string serviceBusConnectionName, int index)
    {
        var connectionByName = ServiceBusConnectionSettings
            .First(p => p.Name.EqualsInvariantIgnoreCase(serviceBusConnectionName));

        connectionByName.UpdateIndex(index);

        foreach (ServiceBusConnectionSettings connection in ServiceBusConnectionSettings
            .Where(p => !p.Name.EqualsInvariantIgnoreCase(serviceBusConnectionName) &&
                        p.Index >= index)
            .OrderBy(p=>p.Index))
        {
            connection.UpdateIndex(connection.Index + 1);
        }
    }
}