using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using QueueProperties = CrossBusExplorer.ServiceBus.Contracts.Types.QueueProperties;
namespace CrossBusExplorer.ServiceBus.Mappings;

public static class QueueMappings
{
    public static QueueInfo ToQueueInfo(
        this Azure.Messaging.ServiceBus.Administration.QueueProperties properties,
        QueueRuntimeProperties runtimeProperties) =>
        new QueueInfo(
            properties.Name,
            Enum.Parse<QueueStatus>(properties.Status.ToString()),
            runtimeProperties.SizeInBytes,
            runtimeProperties.CreatedAt,
            runtimeProperties.AccessedAt,
            runtimeProperties.UpdatedAt,
            runtimeProperties.ActiveMessageCount,
            runtimeProperties.DeadLetterMessageCount,
            runtimeProperties.ScheduledMessageCount,
            runtimeProperties.TransferMessageCount,
            runtimeProperties.TransferMessageCount,
            runtimeProperties.TotalMessageCount);

    public static QueueDetails ToQueueDetails(
        this Azure.Messaging.ServiceBus.Administration.QueueProperties properties,
        QueueRuntimeProperties runtimeProperties) =>
        new QueueDetails(
            properties.ToQueueInfo(runtimeProperties),
            GetQueueSettings(properties),
            GetQueueTimeSettings(properties),
            GetQueueProperties(properties));

    private static QueueTimeSettings GetQueueTimeSettings(
        Azure.Messaging.ServiceBus.Administration.QueueProperties queue) =>
        new QueueTimeSettings(queue.AutoDeleteOnIdle,
            queue.DefaultMessageTimeToLive,
            queue.DuplicateDetectionHistoryTimeWindow,
            queue.LockDuration);

    private static Contracts.Types.QueueProperties GetQueueProperties(
        Azure.Messaging.ServiceBus.Administration.QueueProperties queue) =>
        new Contracts.Types.QueueProperties(
            queue.MaxSizeInMegabytes,
            queue.MaxDeliveryCount,
            queue.UserMetadata,
            queue.ForwardTo,
            queue.ForwardDeadLetteredMessagesTo);

    private static QueueSettings GetQueueSettings(
        Azure.Messaging.ServiceBus.Administration.QueueProperties queue) =>
        new QueueSettings(
            queue.EnableBatchedOperations,
            queue.DeadLetteringOnMessageExpiration,
            queue.EnablePartitioning,
            queue.RequiresDuplicateDetection,
            queue.RequiresSession);
}