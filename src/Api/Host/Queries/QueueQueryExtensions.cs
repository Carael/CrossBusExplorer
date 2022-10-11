using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Host.Queries;

[ExtendObjectType("Query")]
public class QueueQueryExtensions
{
    public IAsyncEnumerable<QueueInfo> GetQueuesAsync(
        [Service] IQueueService queueService,
        string connectionName,
        CancellationToken cancellationToken)
    {
        return queueService.GetAsync(
            connectionName,
            cancellationToken);
    }

    public Task<QueueDetails> GetQueueAsync(
        [Service] IQueueService queueService,
        string connectionName,
        string name,
        CancellationToken cancellationToken)
    {
        return queueService.GetAsync(
            connectionName,
            name,
            cancellationToken);
    }
}