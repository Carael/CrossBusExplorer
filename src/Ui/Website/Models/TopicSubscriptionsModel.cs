using System.Collections.Generic;
using System.Linq;
using CrossBusExplorer.ServiceBus.Contracts.Types;
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
        Subscriptions.Add(subscriptionInfo);
    }

    public void AddChild(TopicSubscriptionsModel model)
    {
        ChildrenModels.Add(model);
    }
}