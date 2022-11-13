namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record CreateSubscriptionOptions(
    string TopicName,
    string SubscriptionName,
    TimeSpan? LockDuration,
    bool? RequiresSession,
    TimeSpan? DefaultMessageTimeToLive,
    TimeSpan? AutoDeleteOnIdle,
    bool? DeadLetteringOnMessageExpiration,
    bool? EnableDeadLetteringOnFilterEvaluationExceptions,
    int? MaxDeliveryCount,
    bool? EnableBatchedOperations,
    ServiceBusEntityStatus? Status,
    string? ForwardTo,
    string? ForwardDeadLetteredMessagesTo,
    string? UserMetadata);