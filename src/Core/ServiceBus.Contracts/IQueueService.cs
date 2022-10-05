using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Contracts
{
    public interface IQueueService
    {
        IAsyncEnumerable<QueueInfo> GetQueuesAsync(
            string connectionString,
            CancellationToken cancellationToken);
        Task<QueueDetails> GetQueueAsync(
            string name, 
            string connectionString, 
            CancellationToken cancellationToken);
    }
}