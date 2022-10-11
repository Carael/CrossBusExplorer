using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Host.Queries;

[ExtendObjectType("Query")]
public class TopicQueryExtensions
{
    public IAsyncEnumerable<TopicInfo> GetTopicsAsync(
        [Service] ITopicService topicService,
        string connectionName,
        CancellationToken cancellationToken)
    {
        return topicService.GetAsync(
            connectionName,
            cancellationToken);
    }
}