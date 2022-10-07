using System.Runtime.CompilerServices;
using Azure;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;
namespace CrossBusExplorer.ServiceBus;

public class TopicService : ITopicService
{

    public async IAsyncEnumerable<TopicInfo> GetAsync(
        string connectionString,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ServiceBusAdministrationClient administrationClient =
            new ServiceBusAdministrationClient(connectionString);

        AsyncPageable<TopicProperties> topicsPageable =
            administrationClient.GetTopicsAsync(cancellationToken);
        
        IAsyncEnumerator<TopicProperties> enumerator =
            topicsPageable.GetAsyncEnumerator(cancellationToken);
        
        try
        {
            while (await enumerator.MoveNextAsync())
            {
                TopicProperties topic = enumerator.Current;
                Response<TopicRuntimeProperties> runtimePropertiesResponse =
                    await administrationClient.GetTopicRuntimePropertiesAsync(
                        topic.Name,
                        cancellationToken);

                TopicRuntimeProperties topicRuntimeProperties = runtimePropertiesResponse.Value;

                yield return topic.ToTopicInfo(topicRuntimeProperties);
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }
}