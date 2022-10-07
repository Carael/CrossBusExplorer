namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record CreateQueueOptions(
    string Name,
    IReadOnlyList<SharedAccessAuthorizationRule>? AuthorizationRules,
    long MaxSizeInMegabytes,
    TimeSpan? LockDuration,
    bool? RequiresDuplicateDetection,
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
    bool? EnablePartitioning,
    long? MaxMessageSizeInKilobytes,
    string? UserMetadata);