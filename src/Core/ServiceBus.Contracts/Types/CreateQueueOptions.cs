namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record CreateQueueOptions(
    string Name,
    IReadOnlyList<SharedAccessAuthorizationRule>? AuthorizationRules,
    long? MaxSizeInMegabytes,
    TimeSpan? LockDuration,
    bool? RequiresSession,
    TimeSpan? DefaultMessageTimeToLive,
    TimeSpan? AutoDeleteOnIdle,
    bool? DeadLetteringOnMessageExpiration,
    TimeSpan? DuplicateDetectionHistoryTimeWindow,
    int? MaxDeliveryCount,
    bool? EnableBatchedOperations,
    QueueStatus? Status,
    string? ForwardTo,
    string? ForwardDeadLetteredMessagesTo,
    long? MaxMessageSizeInKilobytes,
    string? UserMetadata,
    bool? RequiresDuplicateDetection,
    bool? EnablePartitioning);