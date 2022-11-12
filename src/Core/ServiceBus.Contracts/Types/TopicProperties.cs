namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record TopicProperties(    
    long MaxQueueSizeInMegabytes,
    long? MaxMessageSizeInKilobytes,
    string? UserMetadata);