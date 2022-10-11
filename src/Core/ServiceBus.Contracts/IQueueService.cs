using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Contracts
{
    public interface IQueueService
    {
        IAsyncEnumerable<QueueInfo> GetAsync(
            string connectionName,
            CancellationToken cancellationToken);
        Task<QueueDetails> GetAsync(
            string connectionName,
            string name,
            CancellationToken cancellationToken);
        Task<OperationResult> DeleteAsync(
            string connectionName,
            string name,
            CancellationToken cancellationToken);
        Task<OperationResult<QueueDetails>> CreateAsync(
            string connectionName, 
            CreateQueueOptions options,
            CancellationToken cancellationToken);
        Task<OperationResult<QueueDetails>> CloneAsync(
            string connectionName, 
            string name,
            string sourceName,
            CancellationToken cancellationToken);
        Task<OperationResult<QueueDetails>> UpdateAsync(
            string connectionName, 
            UpdateQueueOptions options,
            CancellationToken cancellationToken);
    }
}