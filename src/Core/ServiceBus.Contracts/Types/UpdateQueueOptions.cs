namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record UpdateQueueOptions(
    string Name,
    IReadOnlyList<SharedAccessAuthorizationRule>? AuthorizationRules=null,
    long? MaxSizeInMegabytes=null,
    TimeSpan? LockDuration=null,
    bool? RequiresSession=null,
    TimeSpan? DefaultMessageTimeToLive=null,
    TimeSpan? AutoDeleteOnIdle=null,
    bool? DeadLetteringOnMessageExpiration=null,
    TimeSpan? DuplicateDetectionHistoryTimeWindow=null,
    int? MaxDeliveryCount=null,
    bool? EnableBatchedOperations=null,
    QueueStatus? Status=null,
    string? ForwardTo=null,
    string? ForwardDeadLetteredMessagesTo=null,
    long? MaxMessageSizeInKilobytes=null,
    string? UserMetadata=null);