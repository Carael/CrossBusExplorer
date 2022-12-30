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
        bool up = false;
        foreach (ServiceBusConnectionSettings settings in
            ServiceBusConnectionSettings.OrderBy(p => p.Index))
        {
            if (up)
            {
                settings.UpdateIndex(settings.Index + 1);
            }
            
            if (settings.Name.EqualsInvariantIgnoreCase(serviceBusConnectionName))
            {
                settings.UpdateIndex(index);
                up = true;
            }
        }
    }
}