namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record TopicProperties(    
    long MaxQueueSizeInMegabytes,
    long? MaxMessageSizeInKilobytes,
    int MaxDeliveryCount,
    string? UserMetadata,
    string? ForwardTo,
    string? ForwardDeadLetteredMessagesTo);