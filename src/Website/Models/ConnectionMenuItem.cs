using System.Collections.Generic;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.Management;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Website.Models;

public class ConnectionMenuItem
{
    public ConnectionMenuItem(ServiceBusConnection connection)
    {
        Connection = connection;
    }
    
    public ServiceBusConnection Connection { get; }
    public IList<QueueInfo> Queues { get; set; } = new List<QueueInfo>();
    public IList<TopicInfo> Topics { get; set; } = new List<TopicInfo>();
    public bool IsExpanded { get; set; }
    public bool Loaded { get; set; }
}