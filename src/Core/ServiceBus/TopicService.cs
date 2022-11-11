using System.Runtime.CompilerServices;
using Azure;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;
using Microsoft.Azure.Amqp.Serialization;
using TopicProperties = Azure.Messaging.ServiceBus.Administration.TopicProperties;
namespace CrossBusExplorer.ServiceBus;

public class TopicService : ITopicService
{
    private readonly IConnectionManagement _connectionManagement;
    
    public TopicService(IConnectionManagement connectionManagement)
    {
        _connectionManagement = connectionManagement;
    }
    
    public async IAsyncEnumerable<TopicInfo> GetAsync(
        string connectionName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connection = 
            await _connectionManagement.GetAsync(connectionName, cancellationToken);
        
        ServiceBusAdministrationClient administrationClient =
            new ServiceBusAdministrationClient(connection.ConnectionString);

        AsyncPageable<TopicProperties> topicsPageable =
            administrationClient.GetTopicsAsync(cancellationToken);

        IAsyncEnumerator<TopicProperties> enumerator =
            topicsPageable.GetAsyncEnumerator(cancellationToken);

        var topicsToNest = new List<TopicInfo>();

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

                var topicInfo = topic.ToTopicInfo(topicRuntimeProperties);

                if (!topicInfo.IsFolder)
                {
                    yield return topicInfo;
                }
                else
                {
                    topicsToNest.Add(topicInfo);
                }
            }

            var nestedTopicList = GetNestedTopicsList(topicsToNest);

            foreach (var nestedTopic in nestedTopicList)
            {
                yield return nestedTopic;
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }
    private IList<TopicInfo> GetNestedTopicsList(List<TopicInfo> topicsToNest)
    {
        var nestedTopics = new List<TopicInfo>();

        foreach (TopicInfo topicInfo in topicsToNest)
        {
            var nameParts = topicInfo.Name.Split("/");

            TryAdd(nameParts, nestedTopics);
        }

        return nestedTopics;
    }

    private void TryAdd(string[] nameParts, IList<TopicInfo> topics)
    {
        var currentTopic = topics.FirstOrDefault(p => p.Name == nameParts[0]);

        for (var i = 0; i < nameParts.Length; i++)
        {
            var isFolder = i < nameParts.Length - 1;

            if (currentTopic == null)
            {
                currentTopic = new TopicInfo(
                    nameParts[i],
                    isFolder,
                    !isFolder ? string.Join("/", nameParts) : null,
                    new List<TopicInfo>());
                topics.Add(currentTopic);
            }
            else
            {
                if (currentTopic.Name != nameParts[i])
                {
                    var newTopic = new TopicInfo(nameParts[i],
                        isFolder,
                        !isFolder ? string.Join("/", nameParts) : null,
                        new List<TopicInfo>());

                    currentTopic.ChildTopics.Add(newTopic);

                    currentTopic = newTopic;
                }
            }
        }
    }
}