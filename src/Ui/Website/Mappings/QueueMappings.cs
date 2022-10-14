using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Pages;
namespace CrossBusExplorer.Website.Mappings;

public static class QueueMappings
{
    public static QueueFormModel ToFormModel(this QueueDetails details)
    {
        return new QueueFormModel(OperationType.Update)
        {
            Name = details.Info.Name,
            MaxSizeInMegabytes = details.Properties.MaxQueueSizeInMegabytes,
            MaxDeliveryCount = details.Properties.MaxDeliveryCount,
            UserMetadata = details.Properties.UserMetadata,
            ForwardTo = details.Properties.ForwardTo,
            ForwardDeadLetteredMessagesTo = details.Properties.ForwardDeadLetteredMessagesTo,
            DuplicateDetectionHistoryTimeWindow = details.TimeSettings
                .DuplicateDetectionHistoryTimeWindow.ToTimeSpanString(),
            AutoDeleteOnIdle = details.TimeSettings.AutoDeleteOnIdle.ToTimeSpanString(),
            DefaultMessageTimeToLive =
                details.TimeSettings.DefaultMessageTimeToLive.ToTimeSpanString(),
            LockDuration = details.TimeSettings.LockDuration.ToTimeSpanString(),
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
            LockDuration: model.LockDuration.ToTimeSpan(),
            RequiresSession: model.RequiresSession,
            DefaultMessageTimeToLive: model.DefaultMessageTimeToLive.ToTimeSpan(),
            AutoDeleteOnIdle: model.AutoDeleteOnIdle.ToTimeSpan(),
            DeadLetteringOnMessageExpiration: model.DeadLetteringOnMessageExpiration,
            DuplicateDetectionHistoryTimeWindow: model.DuplicateDetectionHistoryTimeWindow
                .ToTimeSpan(),
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
                LockDuration: model.LockDuration.ToTimeSpan(),
                RequiresSession: model.RequiresSession,
                DefaultMessageTimeToLive: model.DefaultMessageTimeToLive.ToTimeSpan(),
                AutoDeleteOnIdle: model.AutoDeleteOnIdle.ToTimeSpan(),
                DeadLetteringOnMessageExpiration: model.DeadLetteringOnMessageExpiration,
                DuplicateDetectionHistoryTimeWindow: model.DuplicateDetectionHistoryTimeWindow
                    .ToTimeSpan(),
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