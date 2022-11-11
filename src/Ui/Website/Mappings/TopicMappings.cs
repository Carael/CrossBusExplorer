using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Pages;
namespace CrossBusExplorer.Website.Mappings;

public static class TopicMappings
{
        public static TopicFormModel ToFormModel(this TopicDetails details, OperationType operationType)
    {
        return new TopicFormModel(operationType)
        {
            Name = operationType == OperationType.Update ? details.Info.Name : null,
            MaxSizeInMegabytes = details.Properties.MaxQueueSizeInMegabytes,
            UserMetadata = details.Properties.UserMetadata,
            DuplicateDetectionHistoryTimeWindow = details.TimeSettings
                .DuplicateDetectionHistoryTimeWindow,
            AutoDeleteOnIdle = details.TimeSettings.AutoDeleteOnIdle,
            DefaultMessageTimeToLive =
                details.TimeSettings.DefaultMessageTimeToLive,
            EnableBatchedOperations = details.Settings.EnableBatchedOperations,
            RequiresDuplicateDetection = details.Settings.RequiresDuplicateDetection,
            EnablePartitioning = details.Settings.EnablePartitioning,
            SupportOrdering = details.Settings.SupportOrdering,
            MaxMessageSizeInKilobytes = details.Properties.MaxMessageSizeInKilobytes
        };
    }

    public static UpdateTopicOptions ToUpdateOptions(this TopicFormModel model)
    {
        return new UpdateTopicOptions(
            model.Name!,
            AuthorizationRules: null,
            MaxSizeInMegabytes: model.MaxSizeInMegabytes,
            DefaultMessageTimeToLive: model.DefaultMessageTimeToLive,
            AutoDeleteOnIdle: model.AutoDeleteOnIdle,
            DuplicateDetectionHistoryTimeWindow: model.DuplicateDetectionHistoryTimeWindow,
            EnableBatchedOperations: model.EnableBatchedOperations,
            SupportOrdering: model.SupportOrdering,
            Status: null,
            MaxMessageSizeInKilobytes: model.MaxMessageSizeInKilobytes,
            UserMetadata: model.UserMetadata);
    }

    public static CreateTopicOptions ToCreateOptions(this TopicFormModel model)
    {
        return new CreateTopicOptions(
            model.Name!,
            AuthorizationRules: null,
            MaxSizeInMegabytes: model.MaxSizeInMegabytes,
            DefaultMessageTimeToLive: model.DefaultMessageTimeToLive,
            AutoDeleteOnIdle: model.AutoDeleteOnIdle,
            DuplicateDetectionHistoryTimeWindow: model.DuplicateDetectionHistoryTimeWindow,
            EnableBatchedOperations: model.EnableBatchedOperations,
            Status: null,
            MaxMessageSizeInKilobytes: model.MaxMessageSizeInKilobytes,
            UserMetadata: model.UserMetadata,
            RequiresDuplicateDetection: model.RequiresDuplicateDetection,
            SupportOrdering: model.SupportOrdering,
            EnablePartitioning: model.EnablePartitioning);
    }
}