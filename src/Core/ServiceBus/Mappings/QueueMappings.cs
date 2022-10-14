using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CreateQueueOptions = CrossBusExplorer.ServiceBus.Contracts.Types.CreateQueueOptions;
using QueueProperties = CrossBusExplorer.ServiceBus.Contracts.Types.QueueProperties;
using SharedAccessAuthorizationRule =
    CrossBusExplorer.ServiceBus.Contracts.Types.SharedAccessAuthorizationRule;
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

    public static Azure.Messaging.ServiceBus.Administration.CreateQueueOptions
        MapToCreateQueueOptions(this CreateQueueOptions createQueueOptions)
    {
        var options =
            new Azure.Messaging.ServiceBus.Administration.CreateQueueOptions(
                createQueueOptions.Name);

        options.MaxSizeInMegabytes = createQueueOptions.MaxSizeInMegabytes ?? 1024;

        if (createQueueOptions.EnablePartitioning.HasValue)
        {
            options.EnablePartitioning = createQueueOptions.EnablePartitioning.Value;
        }

        if (createQueueOptions.LockDuration.HasValue)
        {
            options.LockDuration = createQueueOptions.LockDuration.Value;
        }

        if (createQueueOptions.RequiresDuplicateDetection.HasValue)
        {
            options.RequiresDuplicateDetection =
                createQueueOptions.RequiresDuplicateDetection.Value;
        }

        if (createQueueOptions.RequiresSession.HasValue)
        {
            options.RequiresSession = createQueueOptions.RequiresSession.Value;
        }

        if (createQueueOptions.DefaultMessageTimeToLive.HasValue)
        {
            options.DefaultMessageTimeToLive = createQueueOptions.DefaultMessageTimeToLive.Value;
        }

        if (createQueueOptions.AutoDeleteOnIdle.HasValue)
        {
            options.AutoDeleteOnIdle = createQueueOptions.AutoDeleteOnIdle.Value;
        }

        if (createQueueOptions.DeadLetteringOnMessageExpiration.HasValue)
        {
            options.DeadLetteringOnMessageExpiration =
                createQueueOptions.DeadLetteringOnMessageExpiration.Value;
        }

        if (createQueueOptions.DuplicateDetectionHistoryTimeWindow.HasValue)
        {
            options.DuplicateDetectionHistoryTimeWindow =
                createQueueOptions.DuplicateDetectionHistoryTimeWindow.Value;
        }

        if (createQueueOptions.MaxDeliveryCount.HasValue)
        {
            options.MaxDeliveryCount = createQueueOptions.MaxDeliveryCount.Value;
        }

        if (createQueueOptions.EnableBatchedOperations.HasValue)
        {
            options.EnableBatchedOperations = createQueueOptions.EnableBatchedOperations.Value;
        }

        if (createQueueOptions.Status.HasValue)
        {
            options.Status = new EntityStatus(createQueueOptions.Status.ToString());
        }

        if (createQueueOptions.ForwardTo != null)
        {
            options.ForwardTo = createQueueOptions.ForwardTo;
        }

        if (createQueueOptions.ForwardDeadLetteredMessagesTo != null)
        {
            options.ForwardDeadLetteredMessagesTo =
                createQueueOptions.ForwardDeadLetteredMessagesTo;
        }

        if (createQueueOptions.EnablePartitioning.HasValue)
        {
            options.EnablePartitioning = createQueueOptions.EnablePartitioning.Value;
        }

        if (createQueueOptions.MaxMessageSizeInKilobytes.HasValue)
        {
            options.MaxMessageSizeInKilobytes = createQueueOptions.MaxMessageSizeInKilobytes.Value;
        }

        if (createQueueOptions.UserMetadata != null)
        {
            options.UserMetadata = createQueueOptions.UserMetadata;
        }

        if (createQueueOptions.AuthorizationRules != null)
        {
            foreach (var authorizationRule in createQueueOptions.AuthorizationRules)
            {
                options.AuthorizationRules.Add(
                    authorizationRule.MapToSharedAccessAuthorizationRule());
            }
        }

        return options;
    }

    public static Azure.Messaging.ServiceBus.Administration.QueueProperties UpdateFromOptions(
        this Azure.Messaging.ServiceBus.Administration.QueueProperties queue,
        UpdateQueueOptions options)
    {
        if (options.MaxSizeInMegabytes != null)
        {
            queue.MaxSizeInMegabytes = options.MaxSizeInMegabytes.Value;
        }

        if (options.LockDuration.HasValue)
        {
            queue.LockDuration = options.LockDuration.Value;
        }

        if (options.DefaultMessageTimeToLive.HasValue)
        {
            queue.DefaultMessageTimeToLive = options.DefaultMessageTimeToLive.Value;
        }

        if (options.AutoDeleteOnIdle.HasValue)
        {
            queue.AutoDeleteOnIdle = options.AutoDeleteOnIdle.Value;
        }

        if (options.DeadLetteringOnMessageExpiration.HasValue)
        {
            queue.DeadLetteringOnMessageExpiration =
                options.DeadLetteringOnMessageExpiration.Value;
        }

        if (options.DuplicateDetectionHistoryTimeWindow.HasValue)
        {
            queue.DuplicateDetectionHistoryTimeWindow =
                options.DuplicateDetectionHistoryTimeWindow.Value;
        }

        if (options.MaxDeliveryCount.HasValue)
        {
            queue.MaxDeliveryCount = options.MaxDeliveryCount.Value;
        }

        if (options.EnableBatchedOperations.HasValue)
        {
            queue.EnableBatchedOperations = options.EnableBatchedOperations.Value;
        }

        if (options.Status.HasValue)
        {
            queue.Status = new EntityStatus(options.Status.ToString());
        }

        if (options.ForwardTo != null)
        {
            queue.ForwardTo = options.ForwardTo;
        }

        if (options.ForwardDeadLetteredMessagesTo != null)
        {
            queue.ForwardDeadLetteredMessagesTo =
                options.ForwardDeadLetteredMessagesTo;
        }

        if (options.MaxMessageSizeInKilobytes.HasValue)
        {
            queue.MaxMessageSizeInKilobytes = options.MaxMessageSizeInKilobytes.Value;
        }

        if (options.UserMetadata != null)
        {
            queue.UserMetadata = options.UserMetadata;
        }

        if (options.AuthorizationRules != null)
        {
            queue.AuthorizationRules.Clear();
            
            foreach (var authorizationRule in options.AuthorizationRules)
            {
                queue.AuthorizationRules.Add(
                    authorizationRule.MapToSharedAccessAuthorizationRule());
            }
        }

        return queue;
    }

    private static QueueTimeSettings GetQueueTimeSettings(
        Azure.Messaging.ServiceBus.Administration.QueueProperties queue) =>
        new QueueTimeSettings(queue.AutoDeleteOnIdle,
            queue.DefaultMessageTimeToLive,
            queue.DuplicateDetectionHistoryTimeWindow,
            queue.LockDuration);

    private static QueueProperties GetQueueProperties(
        Azure.Messaging.ServiceBus.Administration.QueueProperties queue) =>
        new QueueProperties(
            queue.MaxSizeInMegabytes,
            queue.MaxMessageSizeInKilobytes,
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

    private static Azure.Messaging.ServiceBus.Administration.SharedAccessAuthorizationRule
        MapToSharedAccessAuthorizationRule(
            this SharedAccessAuthorizationRule rule) =>
        new Azure.Messaging.ServiceBus.Administration.SharedAccessAuthorizationRule(
            rule.KeyName,
            rule.PrimaryKey,
            rule.SecondaryKey,
            rule.Rights.Select(p => (Azure.Messaging.ServiceBus.Administration.AccessRights)p));
}