using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Contracts;

public interface IMessageService
{
    Task<IReadOnlyList<Message>> GetMessagesAsync(
        string connectionName,
        string queueOrTopicName,
        string? subscriptionName,
        SubQueue subQueue,
        ReceiveMode mode,
        ReceiveType type,
        int? messagesCount,
        long? fromSequenceNumber,
        CancellationToken cancellationToken);

    Task<Result> SendMessagesAsync(
        string connectionName,
        string queueOrTopicName,
        IReadOnlyList<SendMessage> messages,
        CancellationToken cancellationToken);
    
    IAsyncEnumerable<PurgeResult> PurgeAsync(
        string connectionName,
        string topicOrQueueName,
        string? subscriptionName,
        SubQueue subQueue,
        CancellationToken cancellationToken);
    
    IAsyncEnumerable<ResendResult> ResendAsync(string connectionName,
        string topicOrQueueName,
        string? subscriptionName,
        SubQueue subQueue,
        string destinationTopicOrQueueName,
        CancellationToken cancellationToken);
}