using System.Runtime.CompilerServices;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;
using QueueProperties = Azure.Messaging.ServiceBus.Administration.QueueProperties;

namespace CrossBusExplorer.ServiceBus;

public class QueueService : IQueueService
{
    public async IAsyncEnumerable<QueueInfo> GetAsync(
        string connectionString,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ServiceBusAdministrationClient administrationClient =
            new ServiceBusAdministrationClient(connectionString);

        AsyncPageable<QueueProperties> queuesPageable =
            administrationClient.GetQueuesAsync(cancellationToken);

        IAsyncEnumerator<QueueProperties> enumerator =
            queuesPageable.GetAsyncEnumerator(cancellationToken);

        try
        {
            while (await enumerator.MoveNextAsync())
            {
                QueueProperties queue = enumerator.Current;
                Response<QueueRuntimeProperties> runtimePropertiesResponse =
                    await administrationClient.GetQueueRuntimePropertiesAsync(
                        queue.Name,
                        cancellationToken);

                QueueRuntimeProperties queueRuntimeProperties = runtimePropertiesResponse.Value;

                yield return queue.ToQueueInfo(queueRuntimeProperties);
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }
    public async Task<QueueDetails> GetAsync(
        string connectionString,
        string name,
        CancellationToken cancellationToken)
    {
        ServiceBusAdministrationClient administrationClient =
            new ServiceBusAdministrationClient(connectionString);

        var queueResponse = await administrationClient.GetQueueAsync(name, cancellationToken);

        var queue = queueResponse.Value;

        Response<QueueRuntimeProperties> runtimePropertiesResponse =
            await administrationClient.GetQueueRuntimePropertiesAsync(
                queue.Name,
                cancellationToken);

        return queue.ToQueueDetails(runtimePropertiesResponse.Value);
    }
    public async Task<OperationResult> DeleteAsync(
        string connectionString,
        string name,
        CancellationToken cancellationToken)
    {
        try
        {
            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connectionString);

            var response = await administrationClient.DeleteQueueAsync(name, cancellationToken);

            return new OperationResult(!response.IsError);
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
    }
    public async Task<OperationResult<QueueDetails>> CreateAsync(
        string connectionString,
        string name,
        CancellationToken cancellationToken)
    {
        //TODO: support CreateQueueOptions
        try
        {
            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connectionString);
            
            var response = await administrationClient.CreateQueueAsync(
                name,
                cancellationToken);

            Response<QueueRuntimeProperties> runtimePropertiesResponse =
                await administrationClient.GetQueueRuntimePropertiesAsync(
                    response.Value.Name,
                    cancellationToken);
            
            return new OperationResult<QueueDetails>(
                true,
                response.Value.ToQueueDetails(runtimePropertiesResponse.Value));
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
    }
}