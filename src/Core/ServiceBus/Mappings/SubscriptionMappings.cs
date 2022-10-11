using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Mappings;

public static class SubscriptionMappings
{
    public static SubscriptionInfo ToSubscriptionInfo(
        this SubscriptionProperties subscription,
        SubscriptionRuntimeProperties properties)
    {
        return new SubscriptionInfo(
            subscription.SubscriptionName,
            properties.ActiveMessageCount,
            properties.DeadLetterMessageCount,
            properties.TransferMessageCount);
    }
}