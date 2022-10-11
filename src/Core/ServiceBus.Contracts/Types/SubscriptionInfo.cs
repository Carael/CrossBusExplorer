namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record SubscriptionInfo(
    string Name,
    long ActiveMessagesCount,
    long DeadLetterMessagesCount,
    long TransferMessagesCount);