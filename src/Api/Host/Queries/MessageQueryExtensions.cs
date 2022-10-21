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
        SubQueue subQueue,
        ReceiveMode mode,
        ReceiveType type,
        int? messagesCount,
        long? fromSequenceNumber,
        CancellationToken cancellationToken)
    {
        return messageService.GetMessagesAsync(
            connectionName,
            queueOrTopicName,
            subscriptionName,
            subQueue,
            mode,
            type,
            messagesCount,
            fromSequenceNumber,
            cancellationToken);
    }
}