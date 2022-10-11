using Azure.Messaging.ServiceBus;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Host.Mutations;

[ExtendObjectType("Mutation")]
public class QueueMutationExtensions
{
    [UseMutationConvention(PayloadFieldName = "result")]
    [Error<ServiceBusOperationException>]
    public async Task<OperationResult> DeleteQueueAsync(
        [Service] IQueueService queueService,
        string connectionName,
        string name,
        CancellationToken cancellationToken)
    {
        return await queueService.DeleteAsync(connectionName, name, cancellationToken);
    }

    [UseMutationConvention(PayloadFieldName = "result")]
    [Error<ServiceBusOperationException>]
    public async Task<OperationResult<QueueDetails>> UpdateQueueAsync(
        [Service] IQueueService queueService,
        string connectionName,
        UpdateQueueOptions options,
        CancellationToken cancellationToken)
    {
        return await queueService.UpdateAsync(connectionName, options, cancellationToken);
    }
    
    [UseMutationConvention(PayloadFieldName = "result")]
    [Error<ServiceBusOperationException>]
    [Error<ValidationException>]
    public async Task<OperationResult<QueueDetails>> CreateQueueAsync(
        [Service] IQueueService queueService,
        string connectionName,
        CreateQueueOptions options,
        CancellationToken cancellationToken)
    {
        return await queueService.CreateAsync(connectionName, options, cancellationToken);
    }
    
    [UseMutationConvention(PayloadFieldName = "result")]
    [Error<ServiceBusOperationException>]
    public async Task<OperationResult<QueueDetails>> CloneQueueAsync(
        [Service] IQueueService queueService,
        string connectionName,
        string name,
        string sourceName,
        CancellationToken cancellationToken)
    {
        return await queueService.CloneAsync(connectionName, name, sourceName, cancellationToken);
    }
}