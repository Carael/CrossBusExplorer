using System.Collections.Generic;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Website.Models;

public record TopicSubscriptionsModel(
    string ConnectionName,
    TopicInfo Topic,
    IReadOnlyList<SubscriptionInfo>? Subscriptions);