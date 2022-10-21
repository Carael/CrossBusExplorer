using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Pages;
namespace CrossBusExplorer.Website.Mappings;

public static class QueueMappings
{
    public static QueueFormModel ToFormModel(this QueueDetails details, OperationType operationType)
    {
        return new QueueFormModel(operationType)
        {
            Name = operationType == OperationType.Update ? details.Info.Name : null,
            MaxSizeInMegabytes = details.Properties.MaxQueueSizeInMegabytes,
            MaxDeliveryCount = details.Properties.MaxDeliveryCount,
            UserMetadata = details.Properties.UserMetadata,
            ForwardTo = details.Properties.ForwardTo,
            ForwardDeadLetteredMessagesTo = details.Properties.ForwardDeadLetteredMessagesTo,
            DuplicateDetectionHistoryTimeWindow = details.TimeSettings
                .DuplicateDetectionHistoryTimeWindow,
            AutoDeleteOnIdle = details.TimeSettings.AutoDeleteOnIdle,
            DefaultMessageTimeToLive =
                details.TimeSettings.DefaultMessageTimeToLive,
            LockDuration = details.TimeSettings.LockDuration,
            RequiresSession = details.Settings.RequiresSession,
            DeadLetteringOnMessageExpiration =
                details.Settings.EnableDeadLetteringOnMessageExpiration,
            EnableBatchedOperations = details.Settings.EnableBatchedOperations,
            RequiresDuplicateDetection = details.Settings.RequiresDuplicateDetection,
            EnablePartitioning = details.Settings.EnablePartitioning,
            MaxMessageSizeInKilobytes = details.Properties.MaxMessageSizeInKilobytes
        };
    }

    public static UpdateQueueOptions ToUpdateOptions(this QueueFormModel model)
    {
        return new UpdateQueueOptions(
            model.Name!,
            AuthorizationRules: null,
            MaxSizeInMegabytes: model.MaxSizeInMegabytes,
            LockDuration: model.LockDuration,
            RequiresSession: model.RequiresSession,
            DefaultMessageTimeToLive: model.DefaultMessageTimeToLive,
            AutoDeleteOnIdle: model.AutoDeleteOnIdle,
            DeadLetteringOnMessageExpiration: model.DeadLetteringOnMessageExpiration,
            DuplicateDetectionHistoryTimeWindow: model.DuplicateDetectionHistoryTimeWindow,
            MaxDeliveryCount: model.MaxDeliveryCount,
            EnableBatchedOperations: model.EnableBatchedOperations,
            Status: null,
            ForwardTo: model.ForwardTo,
            ForwardDeadLetteredMessagesTo: model.ForwardDeadLetteredMessagesTo,
            MaxMessageSizeInKilobytes: model.MaxMessageSizeInKilobytes,
            UserMetadata: model.UserMetadata);
    }

    public static CreateQueueOptions ToCreateOptions(this QueueFormModel model)
    {
        return new CreateQueueOptions(
            model.Name!,
            AuthorizationRules: null,
            MaxSizeInMegabytes: model.MaxSizeInMegabytes,
            LockDuration: model.LockDuration,
            RequiresSession: model.RequiresSession,
            DefaultMessageTimeToLive: model.DefaultMessageTimeToLive,
            AutoDeleteOnIdle: model.AutoDeleteOnIdle,
            DeadLetteringOnMessageExpiration: model.DeadLetteringOnMessageExpiration,
            DuplicateDetectionHistoryTimeWindow: model.DuplicateDetectionHistoryTimeWindow,
            MaxDeliveryCount: model.MaxDeliveryCount,
            EnableBatchedOperations: model.EnableBatchedOperations,
            Status: null,
            ForwardTo: model.ForwardTo,
            ForwardDeadLetteredMessagesTo: model.ForwardDeadLetteredMessagesTo,
            MaxMessageSizeInKilobytes: model.MaxMessageSizeInKilobytes,
            UserMetadata: model.UserMetadata,
            RequiresDuplicateDetection: model.RequiresDuplicateDetection,
            EnablePartitioning: model.EnablePartitioning);
    }

}