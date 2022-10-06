namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record QueueProperties(    
    long MaxSizeInMegabytes,
    int MaxDeliveryCount,
    string? UserMetadata,
    string? ForwardTo,
    string? ForwardDeadLetteredMessagesTo);