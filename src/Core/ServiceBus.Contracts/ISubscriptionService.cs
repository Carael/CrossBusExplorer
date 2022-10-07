using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Contracts;

public interface ISubscriptionService
{
    IAsyncEnumerable<SubscriptionInfo> GetAsync(
        string connectionString,
        string topicName,
        CancellationToken cancellationToken);
}