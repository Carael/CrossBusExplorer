using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Host.Queries;

[ExtendObjectType("Query")]
public class MessageQueryExtensions
{
    public Task<IReadOnlyList<Message>> GetMessagesAsync(
        [Service] IMessageService messageService,
        string connectionName,
        string queueOrTopicName,
        string? subscriptionName,
        int messagesCount,
        ReceiveMode receiveMode,
        long? fromSequenceNumber,
        CancellationToken cancellationToken)
    {
        return messageService.GetMessagesAsync(
            connectionName,
            queueOrTopicName,
            subscriptionName,
            messagesCount,
            receiveMode,
            fromSequenceNumber,
            cancellationToken);
    }
}