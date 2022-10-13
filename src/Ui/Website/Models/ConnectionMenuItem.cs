using System.Collections.Generic;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Website.Models;

public class ConnectionMenuItem
{
    public ConnectionMenuItem(string connectionName)
    {
        ConnectionName = connectionName;
    }
    public string ConnectionName { get; }
    public IList<QueueInfo> Queues { get; } = new List<QueueInfo>();
    public IList<TopicInfo> Topics { get; } = new List<TopicInfo>();
    public bool QueuesLoaded { get; set; }
    public bool TopicsLoaded { get; set; }
    public bool LoadingQueues { get; set; }
    public bool LoadingTopics { get; set; }
}