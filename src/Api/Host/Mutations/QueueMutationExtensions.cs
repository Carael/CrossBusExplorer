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
        string connectionString,
        string name,
        CancellationToken cancellationToken)
    {
        return await queueService.DeleteAsync(connectionString, name, cancellationToken);
    }

    [UseMutationConvention(PayloadFieldName = "result")]
    [Error<ServiceBusOperationException>]
    public async Task<OperationResult<QueueDetails>> UpdateQueueAsync(
        [Service] IQueueService queueService,
        string connectionString,
        string name,
        CancellationToken cancellationToken)
    {
        return await queueService.UpdateAsync(connectionString, name, cancellationToken);
    }
    
    [UseMutationConvention(PayloadFieldName = "result")]
    [Error<ServiceBusOperationException>]
    [Error<ValidationException>]
    public async Task<OperationResult<QueueDetails>> CreateQueueAsync(
        [Service] IQueueService queueService,
        string connectionString,
        CreateQueueOptions options,
        CancellationToken cancellationToken)
    {
        return await queueService.CreateAsync(connectionString, options, cancellationToken);
    }
    
    [UseMutationConvention(PayloadFieldName = "result")]
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