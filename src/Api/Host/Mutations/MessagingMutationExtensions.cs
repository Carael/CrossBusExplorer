using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Host.Mutations;

[ExtendObjectType("Mutation")]
public class MessagingMutationExtensions
{
    [UseMutationConvention(PayloadFieldName = "result")]
    public async Task<Removed> PurgeAsync(
        [Service] IMessageService messageService,
        string connectionString,
        string queueName,
        SubQueue subQueue,
        CancellationToken cancellationToken)
    {
        return await messageService.PurgeAsync(
            connectionString,
            queueName,
            subQueue,
            cancellationToken);
    }
    
    [UseMutationConvention(PayloadFieldName = "result")]
    public async Task<Sent> SendMessagesAsync(
        [Service] IMessageService messageService,
        string connectionString,
        string queueOrTopicName,
        IReadOnlyList<SendMessage> messages,
        CancellationToken cancellationToken)
    {
        return await messageService.SendMessagesAsync(
            connectionString,
            queueOrTopicName,
            messages,
            cancellationToken);
    }
}