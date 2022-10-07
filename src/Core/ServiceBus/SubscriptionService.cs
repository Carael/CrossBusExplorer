using System.Runtime.CompilerServices;
using Azure;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;
namespace CrossBusExplorer.ServiceBus;

public class SubscriptionService : ISubscriptionService
{
    public async IAsyncEnumerable<SubscriptionInfo> GetAsync(
        string connectionString,
        string topicName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ServiceBusAdministrationClient administrationClient =
            new ServiceBusAdministrationClient(connectionString);

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