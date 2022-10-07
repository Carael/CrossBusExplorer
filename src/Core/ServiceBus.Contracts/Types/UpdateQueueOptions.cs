namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record UpdateQueueOptions(
    IReadOnlyList<SharedAccessAuthorizationRule>? AuthorizationRules,
    TimeSpan? AutoDeleteOnIdle,
    bool? DeadLetteringOnMessageExpiration,
    TimeSpan? DefaultMessageTimeToLive,
    TimeSpan? DuplicateDetectionHistoryTimeWindow,
    bool? EnableBatchedOperations,
    string? ForwardDeadLetteredMessagesTo,
    string? ForwardTo,
    TimeSpan? LockDuration,
    int? MaxDeliveryCount,
    long? MaxMessageSizeInKilobytes,
    long? MaxSizeInMegabytes,
    string Name,
    bool? RequiresSession,
    QueueStatus? Status,
    string? UserMetadata);