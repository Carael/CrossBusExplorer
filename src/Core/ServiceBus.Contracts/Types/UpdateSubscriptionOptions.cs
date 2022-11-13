namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record UpdateSubscriptionOptions(
    string TopicName,
    string SubscriptionName,
    TimeSpan? LockDuration=null,
    bool? RequiresSession=null,
    TimeSpan? DefaultMessageTimeToLive=null,
    TimeSpan? AutoDeleteOnIdle=null,
    bool? DeadLetteringOnMessageExpiration=null,
    int? MaxDeliveryCount=null,
    bool? EnableBatchedOperations=null,
    ServiceBusEntityStatus? Status=null,
    string? ForwardTo=null,
    string? ForwardDeadLetteredMessagesTo=null,
    string? UserMetadata=null);