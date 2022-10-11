using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Contracts;

public interface ISubscriptionService
{
    IAsyncEnumerable<SubscriptionInfo> GetAsync(
        string connectionName,
        string topicName,
        CancellationToken cancellationToken);
}