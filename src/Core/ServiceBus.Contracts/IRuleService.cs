using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Contracts;

public interface IRuleService
{
    IAsyncEnumerable<Rule> GetAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken);
    
    Task<OperationResult> DeleteAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        string ruleName,
        CancellationToken cancellationToken);
    
    Task<OperationResult<Rule>> CreateAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        string ruleName,
        RuleType type,
        string? value,
        CancellationToken cancellationToken);
    
    Task<OperationResult<Rule>> UpdateAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        string ruleName,
        RuleType type,
        string? value,
        CancellationToken cancellationToken);
}