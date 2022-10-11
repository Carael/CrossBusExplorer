using Azure.Messaging.ServiceBus;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using SubQueue = CrossBusExplorer.ServiceBus.Contracts.Types.SubQueue;
namespace CrossBusExplorer.Host.Mutations;

[ExtendObjectType("Mutation")]
public class MessagingMutationExtensions
{
    [Error<ServiceBusOperationException>]
    [UseMutationConvention(PayloadFieldName = "result")]
    public async Task<Result> PurgeAsync(
        [Service] IMessageService messageService,
        string connectionName,
        string queueName,
        SubQueue subQueue,
        CancellationToken cancellationToken)
    {
        return await messageService.PurgeAsync(
            connectionName,
            queueName,
            subQueue,
            cancellationToken);
    }
    
    [Error<ValidationException>]
    [Error<ServiceBusOperationException>]
    [UseMutationConvention(PayloadFieldName = "result")]
    public async Task<Result> SendMessagesAsync(
        [Service] IMessageService messageService,
        string connectionName,
        string queueOrTopicName,
        IReadOnlyList<SendMessage> messages,
        CancellationToken cancellationToken)
    {
        return await messageService.SendMessagesAsync(
            connectionName,
            queueOrTopicName,
            messages,
            cancellationToken);
    }
}