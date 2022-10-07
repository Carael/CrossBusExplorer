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
        return new TopicInfo(topic.Name);
    }
}