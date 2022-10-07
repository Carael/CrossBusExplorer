using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Contracts;

public interface IMessageService
{
    Task<IReadOnlyList<Message>> GetMessagesAsync(
        string connectionString,
        string queueOrTopicName,
        string? subscriptionName,
        int messagesCount,
        ReceiveMode receiveMode,
        long? fromSequenceNumber,
        CancellationToken cancellationToken);

    Task<Result> PurgeAsync(string connectionString,
        string name,
        SubQueue subQueue,
        CancellationToken cancellationToken);

    Task<Result> SendMessagesAsync(
        string connectionString,
        string queueOrTopicName,
        IReadOnlyList<SendMessage> messages,
        CancellationToken cancellationToken);
}