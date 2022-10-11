using System.Runtime.CompilerServices;
using Azure;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;
using Microsoft.Azure.Amqp.Serialization;
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

        // foreach (TopicInfo topicInfo in topicsToGroup)
        // {
        //     yield return topicInfo;
        // }
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

        // var topic = topics.FirstOrDefault(p => p.Name == name);
        //     if (topic == null)
        //     {
        //         var topic = new TopicInfo(topic,true, new List<TopicInfo>();
        //     }
        // }
    }
}