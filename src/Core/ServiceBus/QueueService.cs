using System.Runtime.CompilerServices;
using Azure;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using QueueProperties = Azure.Messaging.ServiceBus.Administration.QueueProperties;

namespace CrossBusExplorer.ServiceBus;

public class QueueService : IQueueService
{
    public async IAsyncEnumerable<QueueInfo> GetQueuesAsync(
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
            new ServiceBusAdministrationClient(connectionString);

        var queueResponse = await administrationClient.GetQueueAsync(name, cancellationToken);

        var queue = queueResponse.Value;

        Response<QueueRuntimeProperties> runtimePropertiesResponse =
            await administrationClient.GetQueueRuntimePropertiesAsync(
                queue.Name,
                cancellationToken);

        QueueRuntimeProperties queueRuntimeProperties = runtimePropertiesResponse.Value;

        return new QueueDetails(
            GetQueueInfo(queue, queueRuntimeProperties),
            GetQueueSettings(queue),
            GetQueueTimeSettings(queue),
            GetQueueProperties(queue));
    }
    private QueueTimeSettings GetQueueTimeSettings(QueueProperties queue) =>
        new QueueTimeSettings(queue.AutoDeleteOnIdle,
            queue.DefaultMessageTimeToLive,
            queue.DuplicateDetectionHistoryTimeWindow,
            queue.LockDuration);

    private Contracts.Types.QueueProperties GetQueueProperties(QueueProperties queue) =>
        new Contracts.Types.QueueProperties(
            queue.MaxSizeInMegabytes,
            queue.MaxDeliveryCount,
            queue.UserMetadata,
            queue.ForwardTo,
            queue.ForwardDeadLetteredMessagesTo);

    private QueueSettings GetQueueSettings(QueueProperties queue) =>
        new QueueSettings(
            queue.EnableBatchedOperations,
            queue.DeadLetteringOnMessageExpiration,
            queue.EnablePartitioning,
            queue.RequiresDuplicateDetection,
            queue.RequiresSession);


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
}