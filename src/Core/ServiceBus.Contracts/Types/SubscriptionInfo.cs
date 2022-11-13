namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record SubscriptionInfo(
    string TopicName,
    string SubscriptionName,
    long ActiveMessagesCount,
    long DeadLetterMessagesCount,
    long TransferMessagesCount);