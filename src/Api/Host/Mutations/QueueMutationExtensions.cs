using Azure.Messaging.ServiceBus;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Host.Mutations;

[ExtendObjectType("Mutation")]
public class QueueMutationExtensions
{
    [Error<ServiceBusOperationException>]
    public async Task<OperationResult> DeleteQueueAsync(
        [Service] IQueueService queueService,
        string connectionString,
        string name,
        CancellationToken cancellationToken)
    {
        return await queueService.DeleteAsync(connectionString, name, cancellationToken);
    }

    [Error<ServiceBusOperationException>]
    public async Task<OperationResult<QueueDetails>> UpdateQueueAsync(
        [Service] IQueueService queueService,
        string connectionString,
        string name,
        CancellationToken cancellationToken)
    {
        return await queueService.UpdateAsync(connectionString, name, cancellationToken);
    }

    [UseMutationConvention()]
    [Error<ServiceBusOperationException>]
    public async Task<OperationResult<QueueDetails>> CreateQueueAsync(
        [Service] IQueueService queueService,
        string connectionString,
        CreateQueueOptions options,
        CancellationToken cancellationToken)
    {
        return await queueService.CreateAsync(connectionString, options, cancellationToken);
    }

    [Error<ServiceBusOperationException>]
    public async Task<OperationResult<QueueDetails>> CloneQueueAsync(
        [Service] IQueueService queueService,
        string connectionString,
        string name,
        string sourceName,
        CancellationToken cancellationToken)
    {
        return await queueService.CloneAsync(connectionString, name, sourceName, cancellationToken);
    }
}