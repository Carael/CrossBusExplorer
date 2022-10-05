using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Host.Queries;

[ExtendObjectType("Query")]
public class MessageExtensions
{
    public Task<IReadOnlyList<Message>> GetMessagesAsync(
        [Service] IMessageService messageService,
        string connectionString,
        string queueName,
        int messagesCount,
        ReceiveMode receiveMode,
        long? fromSequenceNumber,
        CancellationToken cancellationToken)
    {
        return messageService.GetMessagesAsync(
            connectionString,
            queueName,
            messagesCount,
            receiveMode,
            fromSequenceNumber,
            cancellationToken);
    }
}