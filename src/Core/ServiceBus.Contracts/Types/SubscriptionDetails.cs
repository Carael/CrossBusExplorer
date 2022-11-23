namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record SubscriptionDetails(
    SubscriptionInfo Info,
    SubscriptionSettings Settings,
    SubscriptionTimeSettings TimeSettings,
    SubscriptionProperties Properties);