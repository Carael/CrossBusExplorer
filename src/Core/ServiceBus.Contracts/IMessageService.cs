using System.Runtime.CompilerServices;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Contracts;

public interface IMessageService
{
    Task<IReadOnlyList<Message>> GetMessagesAsync(
        string connectionString,
        string queueName,
        int messagesCount,
        ReceiveMode receiveMode,
        long? fromSequenceNumber,
        CancellationToken cancellationToken);

    Task<Removed> PurgeAsync(string connectionString,
        string name,
        SubQueue subQueue,
        CancellationToken cancellationToken);
}