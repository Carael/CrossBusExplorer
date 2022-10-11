using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Mappings;

public static class TopicMappings
{
    public static TopicInfo ToTopicInfo(
        this TopicProperties topic,
        TopicRuntimeProperties properties)
    {
        //todo: map properties
        var folders = topic.Name.Split("/");

        if (folders.Length > 1)
        {
            return new TopicInfo(topic.Name, true, null, new List<TopicInfo>());
        }
        else
        {
            return new TopicInfo(topic.Name, false, topic.Name, new List<TopicInfo>());
        }
    }
}