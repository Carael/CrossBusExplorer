using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Contracts;

public interface ITopicService
{
    IAsyncEnumerable<TopicInfo> GetAsync(
        string connectionString,
        CancellationToken cancellationToken);
}