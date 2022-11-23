using Azure.Core;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Pages;
namespace CrossBusExplorer.Website.Mappings;

public static class SubscriptionMappings
{
    public static SubscriptionFormModel ToFormModel(this SubscriptionDetails details,
        OperationType operationType)
    {
        return new SubscriptionFormModel(operationType)
        {
            TopicName = details.Info.TopicName,
            SubscriptionName = operationType == OperationType.Update ? details.Info.SubscriptionName
                : null,
            MaxDeliveryCount = details.Properties.MaxDeliveryCount,
            UserMetadata = details.Properties.UserMetadata,
            ForwardTo = details.Properties.ForwardTo,
            ForwardDeadLetteredMessagesTo = details.Properties.ForwardDeadLetteredMessagesTo,
            AutoDeleteOnIdle = details.TimeSettings.AutoDeleteOnIdle,
            DefaultMessageTimeToLive =
                details.TimeSettings.DefaultMessageTimeToLive,
            LockDuration = details.TimeSettings.LockDuration,
            RequiresSession = details.Settings.RequiresSession,
            DeadLetteringOnMessageExpiration =
                details.Settings.EnableDeadLetteringOnMessageExpiration,
            EnableBatchedOperations = details.Settings.EnableBatchedOperations,
            EnableDeadLetteringOnFilterEvaluationExceptions =
                details.Settings.EnableDeadLetteringOnFilterEvaluationExceptions
        };
    }

    public static UpdateSubscriptionOptions ToUpdateOptions(this SubscriptionFormModel model)
    {
        return new UpdateSubscriptionOptions(
            model.TopicName!,
            model.SubscriptionName,
            LockDuration: model.LockDuration,
            RequiresSession: model.RequiresSession,
            DefaultMessageTimeToLive: model.DefaultMessageTimeToLive,
            AutoDeleteOnIdle: model.AutoDeleteOnIdle,
            DeadLetteringOnMessageExpiration: model.DeadLetteringOnMessageExpiration,
            MaxDeliveryCount: model.MaxDeliveryCount,
            EnableBatchedOperations: model.EnableBatchedOperations,
            Status: null,
            ForwardTo: model.ForwardTo,
            ForwardDeadLetteredMessagesTo: model.ForwardDeadLetteredMessagesTo,
            UserMetadata: model.UserMetadata);
    }

    public static CreateSubscriptionOptions ToCreateOptions(this SubscriptionFormModel model)
    {
        return new CreateSubscriptionOptions(
            model.TopicName,
            model.SubscriptionName,
            LockDuration: model.LockDuration,
            RequiresSession: model.RequiresSession,
            DefaultMessageTimeToLive: model.DefaultMessageTimeToLive,
            AutoDeleteOnIdle: model.AutoDeleteOnIdle,
            DeadLetteringOnMessageExpiration: model.DeadLetteringOnMessageExpiration,
            EnableDeadLetteringOnFilterEvaluationExceptions:
            model.EnableDeadLetteringOnFilterEvaluationExceptions,
            MaxDeliveryCount: model.MaxDeliveryCount,
            EnableBatchedOperations: model.EnableBatchedOperations,
            Status: null,
            ForwardTo: model.ForwardTo,
            ForwardDeadLetteredMessagesTo: model.ForwardDeadLetteredMessagesTo,
            UserMetadata: model.UserMetadata);
    }
}