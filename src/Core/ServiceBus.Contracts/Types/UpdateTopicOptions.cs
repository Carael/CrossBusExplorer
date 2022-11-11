namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record UpdateTopicOptions(
    string Name,
    IReadOnlyList<SharedAccessAuthorizationRule>? AuthorizationRules=null,
    long? MaxSizeInMegabytes=null,
    TimeSpan? DefaultMessageTimeToLive=null,
    TimeSpan? AutoDeleteOnIdle=null,
    TimeSpan? DuplicateDetectionHistoryTimeWindow=null,
    bool? EnableBatchedOperations=null,
    bool? SupportOrdering=null,
    ServiceBusEntityStatus? Status=null,
    long? MaxMessageSizeInKilobytes=null,
    string? UserMetadata=null);