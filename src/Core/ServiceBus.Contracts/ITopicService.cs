using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Contracts;

public interface ITopicService
{
    IAsyncEnumerable<TopicStructure> GetStructureAsync(
        string connectionName,
        CancellationToken cancellationToken);
    Task<TopicDetails> GetAsync(
        string connectionName,
        string name,
        CancellationToken cancellationToken);
    Task<OperationResult> DeleteAsync(
        string connectionName,
        string name,
        CancellationToken cancellationToken);
    Task<OperationResult<TopicDetails>> CreateAsync(
        string connectionName, 
        CreateTopicOptions options,
        CancellationToken cancellationToken);
    Task<OperationResult<TopicDetails>> CloneAsync(
        string connectionName, 
        string name,
        string sourceName,
        CancellationToken cancellationToken);
    Task<OperationResult<TopicDetails>> UpdateAsync(
        string connectionName, 
        UpdateTopicOptions options,
        CancellationToken cancellationToken);
}