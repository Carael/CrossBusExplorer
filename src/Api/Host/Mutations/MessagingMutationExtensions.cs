using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Host.Mutations;

[ExtendObjectType("Mutation")]
public class MessagingMutationExtensions
{
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
}