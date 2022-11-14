using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CreateSubscriptionOptions = CrossBusExplorer.ServiceBus.Contracts.Types.CreateSubscriptionOptions;
using SubscriptionProperties = Azure.Messaging.ServiceBus.Administration.SubscriptionProperties;
namespace CrossBusExplorer.ServiceBus.Mappings;

public static class SubscriptionMappings
{
    public static SubscriptionInfo ToSubscriptionInfo(
        this SubscriptionProperties subscription,
        SubscriptionRuntimeProperties properties)
    {
        return new SubscriptionInfo(
            subscription.TopicName,
            subscription.SubscriptionName,
            Enum.Parse<ServiceBusEntityStatus>(subscription.Status.ToString()),
            properties.CreatedAt,
            properties.AccessedAt,
            properties.UpdatedAt,
            properties.ActiveMessageCount,
            properties.DeadLetterMessageCount,
            properties.TransferMessageCount);
    }
    
    public static SubscriptionDetails ToSubscriptionDetails(
        this Azure.Messaging.ServiceBus.Administration.SubscriptionProperties properties,
        SubscriptionRuntimeProperties runtimeProperties) =>
        new SubscriptionDetails(
            properties.ToSubscriptionInfo(runtimeProperties),
            GetSubscriptionSettings(properties),
            GetSubscriptionTimeSettings(properties),
            GetSubscriptionProperties(properties));

    private static SubscriptionSettings GetSubscriptionSettings(
        SubscriptionProperties subscription) =>
        new SubscriptionSettings(
            subscription.EnableBatchedOperations,
            subscription.DeadLetteringOnMessageExpiration,
            subscription.RequiresSession,
            subscription.EnableDeadLetteringOnFilterEvaluationExceptions);
    
    private static SubscriptionTimeSettings GetSubscriptionTimeSettings(
        SubscriptionProperties subscription) =>
        new SubscriptionTimeSettings(
            subscription.AutoDeleteOnIdle,
            subscription.DefaultMessageTimeToLive,
            subscription.LockDuration);
    
    private static CrossBusExplorer.ServiceBus.Contracts.Types.SubscriptionProperties GetSubscriptionProperties(
        SubscriptionProperties subscription) =>
        new CrossBusExplorer.ServiceBus.Contracts.Types.SubscriptionProperties(
            subscription.MaxDeliveryCount,
            subscription.UserMetadata,
            subscription.ForwardTo.RemoveUrl(),
            subscription.ForwardDeadLetteredMessagesTo.RemoveUrl());
    
      public static Azure.Messaging.ServiceBus.Administration.CreateSubscriptionOptions
        MapToCreateSubscriptionOptions(this CreateSubscriptionOptions createSubscriptionOptions)
    {
        var options =
            new Azure.Messaging.ServiceBus.Administration.CreateSubscriptionOptions(
                createSubscriptionOptions.TopicName, 
                createSubscriptionOptions.SubscriptionName);

        if (createSubscriptionOptions.LockDuration.HasValue)
        {
            options.LockDuration = createSubscriptionOptions.LockDuration.Value;
        }

        if (createSubscriptionOptions.RequiresSession.HasValue)
        {
            options.RequiresSession = createSubscriptionOptions.RequiresSession.Value;
        }

        if (createSubscriptionOptions.DefaultMessageTimeToLive.HasValue)
        {
            options.DefaultMessageTimeToLive = createSubscriptionOptions.DefaultMessageTimeToLive.Value;
        }

        if (createSubscriptionOptions.AutoDeleteOnIdle.HasValue)
        {
            options.AutoDeleteOnIdle = createSubscriptionOptions.AutoDeleteOnIdle.Value;
        }

        if (createSubscriptionOptions.DeadLetteringOnMessageExpiration.HasValue)
        {
            options.DeadLetteringOnMessageExpiration =
                createSubscriptionOptions.DeadLetteringOnMessageExpiration.Value;
        }

        if (createSubscriptionOptions.MaxDeliveryCount.HasValue)
        {
            options.MaxDeliveryCount = createSubscriptionOptions.MaxDeliveryCount.Value;
        }

        if (createSubscriptionOptions.EnableBatchedOperations.HasValue)
        {
            options.EnableBatchedOperations = createSubscriptionOptions.EnableBatchedOperations.Value;
        }

        if (createSubscriptionOptions.Status.HasValue)
        {
            options.Status = new EntityStatus(createSubscriptionOptions.Status.ToString());
        }

        if (createSubscriptionOptions.ForwardTo != null)
        {
            options.ForwardTo = createSubscriptionOptions.ForwardTo;
        }

        if (createSubscriptionOptions.ForwardDeadLetteredMessagesTo != null)
        {
            options.ForwardDeadLetteredMessagesTo =
                createSubscriptionOptions.ForwardDeadLetteredMessagesTo;
        }

        if (createSubscriptionOptions.UserMetadata != null)
        {
            options.UserMetadata = createSubscriptionOptions.UserMetadata;
        }

        return options;
    }

    public static SubscriptionProperties UpdateFromOptions(
        this SubscriptionProperties subscription,
        UpdateSubscriptionOptions options)
    {
        if (options.LockDuration.HasValue)
        {
            subscription.LockDuration = options.LockDuration.Value;
        }

        if (options.DefaultMessageTimeToLive.HasValue)
        {
            subscription.DefaultMessageTimeToLive = options.DefaultMessageTimeToLive.Value;
        }

        if (options.AutoDeleteOnIdle.HasValue)
        {
            subscription.AutoDeleteOnIdle = options.AutoDeleteOnIdle.Value;
        }

        if (options.DeadLetteringOnMessageExpiration.HasValue)
        {
            subscription.DeadLetteringOnMessageExpiration =
                options.DeadLetteringOnMessageExpiration.Value;
        }

        if (options.MaxDeliveryCount.HasValue)
        {
            subscription.MaxDeliveryCount = options.MaxDeliveryCount.Value;
        }

        if (options.EnableBatchedOperations.HasValue)
        {
            subscription.EnableBatchedOperations = options.EnableBatchedOperations.Value;
        }

        if (options.Status.HasValue)
        {
            subscription.Status = new EntityStatus(options.Status.ToString());
        }

        if (options.ForwardTo != null)
        {
            subscription.ForwardTo = options.ForwardTo;
        }

        if (options.ForwardDeadLetteredMessagesTo != null)
        {
            subscription.ForwardDeadLetteredMessagesTo =
                options.ForwardDeadLetteredMessagesTo;
        }

        if (options.UserMetadata != null)
        {
            subscription.UserMetadata = options.UserMetadata;
        }

        return subscription;
    }
}