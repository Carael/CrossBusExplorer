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
}