using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Host.Queries;

[ExtendObjectType("Query")]
public class SubscriptionQueryExtensions
{
    public IAsyncEnumerable<SubscriptionInfo> GetSubscriptionsAsync(
        [Service] ISubscriptionService subscriptionService,
        string connectionName,
        string topicName,
        CancellationToken cancellationToken)
    {
        return subscriptionService.GetAsync(
            connectionName,
            topicName,
            cancellationToken);
    }
}