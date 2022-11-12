using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CreateTopicOptions = CrossBusExplorer.ServiceBus.Contracts.Types.CreateTopicOptions;
using TopicProperties = Azure.Messaging.ServiceBus.Administration.TopicProperties;
namespace CrossBusExplorer.ServiceBus.Mappings;

public static class TopicMappings
{
    public static TopicDetails ToTopicDetails(
        this TopicProperties properties,
        TopicRuntimeProperties runtimeProperties) =>
        new TopicDetails(
            properties.ToTopicInfo(runtimeProperties),
            GetTopicSettings(properties),
            GetTopicTimeSettings(properties),
            GetTopicProperties(properties));

    public static TopicInfo ToTopicInfo(
        this TopicProperties properties,
        TopicRuntimeProperties runtimeProperties) =>
        new TopicInfo(
            properties.Name,
            Enum.Parse<ServiceBusEntityStatus>(properties.Status.ToString()),
            runtimeProperties.SizeInBytes,
            runtimeProperties.CreatedAt,
            runtimeProperties.AccessedAt,
            runtimeProperties.UpdatedAt,
            runtimeProperties.ScheduledMessageCount);

    public static Azure.Messaging.ServiceBus.Administration.CreateTopicOptions
        MapToCreateTopicOptions(this CreateTopicOptions createTopicOptions)
    {
        var options =
            new Azure.Messaging.ServiceBus.Administration.CreateTopicOptions(
                createTopicOptions.Name);

        options.MaxSizeInMegabytes = createTopicOptions.MaxSizeInMegabytes ?? 1024;

        if (createTopicOptions.EnablePartitioning.HasValue)
        {
            options.EnablePartitioning = createTopicOptions.EnablePartitioning.Value;
        }

        if (createTopicOptions.RequiresDuplicateDetection.HasValue)
        {
            options.RequiresDuplicateDetection =
                createTopicOptions.RequiresDuplicateDetection.Value;
        }

        if (createTopicOptions.DefaultMessageTimeToLive.HasValue)
        {
            options.DefaultMessageTimeToLive = createTopicOptions.DefaultMessageTimeToLive.Value;
        }

        if (createTopicOptions.AutoDeleteOnIdle.HasValue)
        {
            options.AutoDeleteOnIdle = createTopicOptions.AutoDeleteOnIdle.Value;
        }

        if (createTopicOptions.DuplicateDetectionHistoryTimeWindow.HasValue)
        {
            options.DuplicateDetectionHistoryTimeWindow =
                createTopicOptions.DuplicateDetectionHistoryTimeWindow.Value;
        }

        if (createTopicOptions.EnableBatchedOperations.HasValue)
        {
            options.EnableBatchedOperations = createTopicOptions.EnableBatchedOperations.Value;
        }
        
        if (createTopicOptions.SupportOrdering.HasValue)
        {
            options.SupportOrdering = createTopicOptions.SupportOrdering.Value;
        }

        if (createTopicOptions.Status.HasValue)
        {
            options.Status = new EntityStatus(createTopicOptions.Status.ToString());
        }

        if (createTopicOptions.EnablePartitioning.HasValue)
        {
            options.EnablePartitioning = createTopicOptions.EnablePartitioning.Value;
        }

        if (createTopicOptions.MaxMessageSizeInKilobytes.HasValue)
        {
            options.MaxMessageSizeInKilobytes = createTopicOptions.MaxMessageSizeInKilobytes.Value;
        }

        if (createTopicOptions.UserMetadata != null)
        {
            options.UserMetadata = createTopicOptions.UserMetadata;
        }

        if (createTopicOptions.AuthorizationRules != null)
        {
            foreach (var authorizationRule in createTopicOptions.AuthorizationRules)
            {
                options.AuthorizationRules.Add(
                    authorizationRule.MapToSharedAccessAuthorizationRule());
            }
        }

        return options;
    }

    public static TopicProperties UpdateFromOptions(
        this TopicProperties queue,
        UpdateTopicOptions options)
    {
        if (options.MaxSizeInMegabytes != null)
        {
            queue.MaxSizeInMegabytes = options.MaxSizeInMegabytes.Value;
        }

        if (options.DefaultMessageTimeToLive.HasValue)
        {
            queue.DefaultMessageTimeToLive = options.DefaultMessageTimeToLive.Value;
        }

        if (options.AutoDeleteOnIdle.HasValue)
        {
            queue.AutoDeleteOnIdle = options.AutoDeleteOnIdle.Value;
        }

        if (options.DuplicateDetectionHistoryTimeWindow.HasValue)
        {
            queue.DuplicateDetectionHistoryTimeWindow =
                options.DuplicateDetectionHistoryTimeWindow.Value;
        }

        if (options.EnableBatchedOperations.HasValue)
        {
            queue.EnableBatchedOperations = options.EnableBatchedOperations.Value;
        }

        if (options.EnableBatchedOperations.HasValue)
        {
            queue.SupportOrdering = options.SupportOrdering.Value;
        }

        if (options.Status.HasValue)
        {
            queue.Status = new EntityStatus(options.Status.ToString());
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

    private static TopicSettings GetTopicSettings(
        TopicProperties topic) =>
        new TopicSettings(
            topic.EnableBatchedOperations,
            topic.EnablePartitioning,
            topic.RequiresDuplicateDetection,
            topic.SupportOrdering);

    private static TopicTimeSettings GetTopicTimeSettings(
        TopicProperties queue) =>
        new TopicTimeSettings(queue.AutoDeleteOnIdle,
            queue.DefaultMessageTimeToLive,
            queue.DuplicateDetectionHistoryTimeWindow);

    private static Contracts.Types.TopicProperties GetTopicProperties(TopicProperties topic) =>
        new Contracts.Types.TopicProperties(
            topic.MaxSizeInMegabytes,
            topic.MaxMessageSizeInKilobytes,
            topic.UserMetadata);

    public static TopicStructure ToTopicStructure(this TopicProperties topic)
    {
        var folders = topic.Name.Split("/");

        if (folders.Length > 1)
        {
            return new TopicStructure(topic.Name, true, null, new List<TopicStructure>());
        }

        return new TopicStructure(topic.Name, false, topic.Name, new List<TopicStructure>());
    }
}