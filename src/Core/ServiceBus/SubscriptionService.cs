using System.Runtime.CompilerServices;
using Azure;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;
namespace CrossBusExplorer.ServiceBus;

public class SubscriptionService : ISubscriptionService
{
    private readonly IConnectionManagement _connectionManagement;
    
    public SubscriptionService(IConnectionManagement connectionManagement)
    {
        _connectionManagement = connectionManagement;

    }
    
    public async IAsyncEnumerable<SubscriptionInfo> GetAsync(
        string connectionName,
        string topicName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connection = 
            await _connectionManagement.GetAsync(connectionName, cancellationToken);
        
        ServiceBusAdministrationClient administrationClient =
            new ServiceBusAdministrationClient(connection.ConnectionString);

        AsyncPageable<SubscriptionProperties> subscriptionsPageable =
            administrationClient.GetSubscriptionsAsync(topicName, cancellationToken);
        
        IAsyncEnumerator<SubscriptionProperties> enumerator =
            subscriptionsPageable.GetAsyncEnumerator(cancellationToken);
        
        try
        {
            while (await enumerator.MoveNextAsync())
            {
                SubscriptionProperties subscription = enumerator.Current;
                Response<SubscriptionRuntimeProperties> runtimePropertiesResponse =
                    await administrationClient.GetSubscriptionRuntimePropertiesAsync(
                        subscription.TopicName,
                        subscription.SubscriptionName,
                        cancellationToken);

                SubscriptionRuntimeProperties topicRuntimeProperties = runtimePropertiesResponse.Value;

                yield return subscription.ToSubscriptionInfo(topicRuntimeProperties);
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }
}