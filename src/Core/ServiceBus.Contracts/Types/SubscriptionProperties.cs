namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record SubscriptionProperties(
    int MaxDeliveryCount,
    string? UserMetadata,
    string? ForwardTo,
    string? ForwardDeadLetteredMessagesTo);