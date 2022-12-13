using System.Collections.Generic;
using System.Linq;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
namespace CrossBusExplorer.Website.Models;

public class TopicSubscriptionsModel
{
    public TopicStructure Topic { get; }
    public IList<SubscriptionInfo> Subscriptions { get; }
    public IList<TopicSubscriptionsModel> ChildrenModels { get; }
    public TopicSubscriptionsModel(TopicStructure topic)
    {
        Topic = topic;
        Subscriptions = new List<SubscriptionInfo>();
        ChildrenModels = topic.ChildTopics.Select(p => new TopicSubscriptionsModel(p)).ToList();
    }

    public bool Loaded { get; set; }
    public bool IsLoading { get; set; }

    public void AddSubscription(SubscriptionInfo subscriptionInfo)
    {
        Subscriptions.AddOrReplace(
            p=>p.SubscriptionName.EqualsInvariantIgnoreCase(subscriptionInfo.SubscriptionName), 
            subscriptionInfo);
    }
}