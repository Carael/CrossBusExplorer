namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record QueueProperties(    
    long MaxSizeInMegabytes,
    int MaxDeliveryCount,
    string? UserDescription,
    string? ForwardTo,
    string? ForwardDeadLetteredMessagesTo);