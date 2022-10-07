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
        return queueService.GetAsync(
            connectionString,
            cancellationToken);
    }

    public Task<QueueDetails> GetQueueAsync(
        [Service] IQueueService queueService,
        string connectionString,
        string name,
        CancellationToken cancellationToken)
    {
        return queueService.GetAsync(
            connectionString,
            name,
            cancellationToken);
    }
}