namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record MessageSystemProperties(
    string? ContentType, 
    string? CorrelationId, 
    string? DeadLetterSource, 
    string? DeadLetterReason, 
    string? DeadLetterErrorDescription, 
    int DeliveryCount, 
    long EnqueuedSequenceNumber,
    DateTimeOffset EnqueuedTime,
    DateTimeOffset ExpiresAt,
    DateTimeOffset LockedUntil,
    string LockToken,
    string? PartitionKey,
    string? TransactionPartitionKey,
    string? ReplyTo,
    string? ReplyToSessionId,
    DateTimeOffset? ScheduledEnqueueTime,
    long SequenceNumber,
    string? SessionId,
    MessageState? State,
    TimeSpan TimeToLive,
    string? To);