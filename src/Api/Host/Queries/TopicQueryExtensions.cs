using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Host.Queries;

[ExtendObjectType("Query")]
public class TopicQueryExtensions
{
    public IAsyncEnumerable<TopicStructure> GetTopicsAsync(
        [Service] ITopicService topicService,
        string connectionName,
        CancellationToken cancellationToken)
    {
        return topicService.GetStructureAsync(
            connectionName,
            cancellationToken);
    }
}