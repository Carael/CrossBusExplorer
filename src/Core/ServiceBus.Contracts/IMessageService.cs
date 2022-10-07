using System.Runtime.CompilerServices;
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

    Task<Removed> PurgeAsync(string connectionString,
        string name,
        SubQueue subQueue,
        CancellationToken cancellationToken);

    IAsyncEnumerable<Removed> PurgeAsyncEnumerable(
        string connectionString,
        string name,
        SubQueue subQueue,
        CancellationToken cancellationToken);

    Task<Sent> SendMessagesAsync(
        string connectionString,
        string queueOrTopicName,
        IReadOnlyList<SendMessage> messages,
        CancellationToken cancellationToken);
}