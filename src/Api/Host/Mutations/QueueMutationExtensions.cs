using Azure.Messaging.ServiceBus;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Host.Mutations;

[ExtendObjectType("Mutation")]
public class QueueMutationExtensions
{
    [Error<ServiceBusOperationException>]
    public async Task<OperationResult> DeleteQueueAsync(
        [Service]IQueueService queueService,
        string connectionString,
        string name,
        CancellationToken cancellationToken)
    {
        return await queueService.DeleteAsync(connectionString, name, cancellationToken);
    }
    
    [Error<ServiceBusOperationException>]
    public async Task<OperationResult<QueueDetails>> CreateQueueAsync(
        [Service]IQueueService queueService,
        string connectionString,
        string name,
        CancellationToken cancellationToken)
    {
        return await queueService.CreateAsync(connectionString, name, cancellationToken);
    }
}