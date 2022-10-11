using System.Collections.Generic;
using CrossBusExplorer.Management.Contracts;
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
    public bool QueuesLoaded { get; set; }
    public bool TopicsLoaded { get; set; }
    public bool LoadingQueues { get; set; }
    public bool LoadingTopics { get; set; }
}