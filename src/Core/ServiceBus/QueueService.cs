using System.Runtime.CompilerServices;
using Azure;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;

namespace CrossBusExplorer.ServiceBus;

public class QueueService : IQueueService
{
    public async IAsyncEnumerable<QueueInfo> GetQueuesAsync(
        string connectionString,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ServiceBusAdministrationClient administrationClient =
            await GetClientAsync(connectionString, cancellationToken);

        Azure.AsyncPageable<QueueProperties> queuesPageable =
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

                yield return GetQueueInfo(queue, queueRuntimeProperties);
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }
    public async Task<QueueDetails> GetQueueAsync(
        string name, 
        string connectionString,
        CancellationToken cancellationToken)
    {
        ServiceBusAdministrationClient administrationClient =
            await GetClientAsync(connectionString, cancellationToken);

        var queueResponse = await administrationClient.GetQueueAsync(name, cancellationToken);

        var queue = queueResponse.Value;
        
        Response<QueueRuntimeProperties> runtimePropertiesResponse =
            await administrationClient.GetQueueRuntimePropertiesAsync(
                queue.Name,
                cancellationToken);

        QueueRuntimeProperties queueRuntimeProperties = runtimePropertiesResponse.Value;

        return new QueueDetails(GetQueueInfo(queue, queueRuntimeProperties));

    }
    
    private QueueInfo GetQueueInfo(
        QueueProperties queue,
        QueueRuntimeProperties queueRuntimeProperties) =>
        new QueueInfo(
            queue.Name,
            Enum.Parse<QueueStatus>(queue.Status.ToString()),
            queueRuntimeProperties.SizeInBytes,
            queueRuntimeProperties.CreatedAt,
            queueRuntimeProperties.AccessedAt,
            queueRuntimeProperties.UpdatedAt,
            queueRuntimeProperties.ActiveMessageCount,
            queueRuntimeProperties.DeadLetterMessageCount,
            queueRuntimeProperties.ScheduledMessageCount,
            queueRuntimeProperties.TransferMessageCount,
            queueRuntimeProperties.TransferMessageCount,
            queueRuntimeProperties.TotalMessageCount);

    private async Task<ServiceBusAdministrationClient> GetClientAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        return new ServiceBusAdministrationClient(connectionString);
    }
}