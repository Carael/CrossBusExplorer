using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Host.Queries;

[ExtendObjectType("Query")]
public class MessageQueryExtensions
{
    public Task<IReadOnlyList<Message>> GetMessagesAsync(
        [Service] IMessageService messageService,
        string connectionString,
        string queueOrTopicName,
        string? subscriptionName,
        int messagesCount,
        ReceiveMode receiveMode,
        long? fromSequenceNumber,
        CancellationToken cancellationToken)
    {
        return messageService.GetMessagesAsync(
            connectionString,
            queueOrTopicName,
            subscriptionName,
            messagesCount,
            receiveMode,
            fromSequenceNumber,
            cancellationToken);
    }
}