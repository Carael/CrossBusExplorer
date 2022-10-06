using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Contracts
{
    public interface IQueueService
    {
        IAsyncEnumerable<QueueInfo> GetAsync(
            string connectionString,
            CancellationToken cancellationToken);
        Task<QueueDetails> GetAsync(
            string connectionString,
            string name,
            CancellationToken cancellationToken);
        Task<OperationResult> DeleteAsync(string connectionString,
            string name,
            CancellationToken cancellationToken);
        Task<OperationResult<QueueDetails>> CreateAsync(
            string connectionString, 
            CreateQueueOptions options,
            CancellationToken cancellationToken);
        Task<OperationResult<QueueDetails>> CloneAsync(
            string connectionString, 
            string name,
            string sourceName,
            CancellationToken cancellationToken);
        Task<OperationResult<QueueDetails>> UpdateAsync(
            string connectionString, 
            UpdateQueueOptions options,
            CancellationToken cancellationToken);
    }
}