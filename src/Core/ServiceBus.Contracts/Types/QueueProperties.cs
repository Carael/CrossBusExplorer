namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record QueueProperties(    
    long MaxQueueSizeInMegabytes,
    long? MaxMessageSizeInKilobytes,
    int MaxDeliveryCount,
    string? UserMetadata,
    string? ForwardTo,
    string? ForwardDeadLetteredMessagesTo);