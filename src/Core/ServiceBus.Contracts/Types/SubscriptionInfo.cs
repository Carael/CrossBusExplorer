namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record SubscriptionInfo(
    string TopicName,
    string SubscriptionName,
    ServiceBusEntityStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset AccessedAt,
    DateTimeOffset UpdatedAt,
    long ActiveMessagesCount,
    long DeadLetterMessagesCount,
    long TransferMessagesCount);