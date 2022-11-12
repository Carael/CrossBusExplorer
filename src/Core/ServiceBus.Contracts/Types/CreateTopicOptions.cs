namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record CreateTopicOptions(
    string Name,
    IReadOnlyList<SharedAccessAuthorizationRule>? AuthorizationRules,
    long? MaxSizeInMegabytes,
    TimeSpan? DefaultMessageTimeToLive,
    TimeSpan? AutoDeleteOnIdle,
    TimeSpan? DuplicateDetectionHistoryTimeWindow,
    bool? EnableBatchedOperations,
    ServiceBusEntityStatus? Status,
    long? MaxMessageSizeInKilobytes,
    string? UserMetadata,
    bool? RequiresDuplicateDetection,
    bool? SupportOrdering,
    bool? EnablePartitioning);