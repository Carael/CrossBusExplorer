using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Contracts;

public interface ISubscriptionService
{
    IAsyncEnumerable<SubscriptionInfo> GetAsync(
        string connectionName,
        string topicName,
        CancellationToken cancellationToken);

    Task<SubscriptionDetails> GetAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken);
    Task<OperationResult> DeleteAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken);
    Task<OperationResult<SubscriptionDetails>> CreateAsync(
        string connectionName,
        CreateSubscriptionOptions options,
        CancellationToken cancellationToken);
    Task<OperationResult<SubscriptionDetails>> CloneAsync(
        string connectionName,
        string subscriptionName,
        string sourceTopicName,
        string sourceSubscriptionName,
        CancellationToken cancellationToken);
    Task<OperationResult<SubscriptionDetails>> UpdateAsync(
        string connectionName,
        UpdateSubscriptionOptions options,
        CancellationToken cancellationToken);
}