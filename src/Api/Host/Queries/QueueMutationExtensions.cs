using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Host.Queries;

[ExtendObjectType("Query")]
public class QueueQueryExtensions
{
    public IAsyncEnumerable<QueueInfo> GetQueuesAsync(
        [Service] IQueueService queueService,
        string connectionString,
        CancellationToken cancellationToken)
    {
        return queueService.GetQueuesAsync(
            connectionString,
            cancellationToken);
    }

    public Task<QueueDetails> GetQueueAsync(
        [Service] IQueueService queueService,
        string name,
        string connectionString,
        CancellationToken cancellationToken)
    {
        return queueService.GetQueueAsync(
            name,
            connectionString,
            cancellationToken);
    }
}