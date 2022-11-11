using System.Runtime.CompilerServices;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;
using CreateTopicOptions = CrossBusExplorer.ServiceBus.Contracts.Types.CreateTopicOptions;
using TopicProperties = Azure.Messaging.ServiceBus.Administration.TopicProperties;
namespace CrossBusExplorer.ServiceBus;

public class TopicService : ITopicService
{
    private readonly IConnectionManagement _connectionManagement;
    
    public TopicService(IConnectionManagement connectionManagement)
    {
        _connectionManagement = connectionManagement;
    }
    
    public async IAsyncEnumerable<TopicStructure> GetStructureAsync(
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

        var topicsToNest = new List<TopicStructure>();

        try
        {
            while (await enumerator.MoveNextAsync())
            {
                TopicProperties topic = enumerator.Current;
                var topicStructure = topic.ToTopicStructure();

                if (!topicStructure.IsFolder)
                {
                    yield return topicStructure;
                }
                else
                {
                    topicsToNest.Add(topicStructure);
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
    
    public async Task<TopicDetails> GetAsync(
        string connectionName, 
        string name,
        CancellationToken cancellationToken)
    {
        var connection = await _connectionManagement.GetAsync(connectionName, cancellationToken);
        
        ServiceBusAdministrationClient administrationClient =
            new ServiceBusAdministrationClient(connection.ConnectionString);

        var topicResponse = await administrationClient.GetTopicAsync(name, cancellationToken);

        var topic = topicResponse.Value;

        Response<TopicRuntimeProperties> runtimePropertiesResponse =
            await administrationClient.GetTopicRuntimePropertiesAsync(
                topic.Name,
                cancellationToken);

        return topic.ToTopicDetails(runtimePropertiesResponse.Value);
    }
    
    public async Task<OperationResult> DeleteAsync(
        string connectionName, 
        string name, 
        CancellationToken cancellationToken)
    {
        try
        {
            var connection = 
                await _connectionManagement.GetAsync(connectionName, cancellationToken);
            
            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connection.ConnectionString);

            var response = await administrationClient.DeleteTopicAsync(name, cancellationToken);

            return new OperationResult(!response.IsError);
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
    }
    
    public async Task<OperationResult<TopicDetails>> CreateAsync(
        string connectionName, 
        CreateTopicOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection = 
                await _connectionManagement.GetAsync(connectionName, cancellationToken);
            
            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connection.ConnectionString);

            var createTopicOptions = options.MapToCreateTopicOptions();

            var response = await administrationClient.CreateTopicAsync(
                createTopicOptions,
                cancellationToken);

            Response<TopicRuntimeProperties> runtimePropertiesResponse =
                await administrationClient.GetTopicRuntimePropertiesAsync(
                    response.Value.Name,
                    cancellationToken);

            return new OperationResult<TopicDetails>(
                true,
                response.Value.ToTopicDetails(runtimePropertiesResponse.Value));
        }
        catch (ServiceBusException ex)
        {
            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
        catch (ArgumentException ex)
        {
            throw new ValidationException(ErrorCodes.InvalidArgument, ex.Message);
        }
    }
    
    public async Task<OperationResult<TopicDetails>> CloneAsync(
        string connectionName, 
        string name,
        string sourceName,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection = 
                await _connectionManagement.GetAsync(connectionName, cancellationToken);
            
            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connection.ConnectionString);

            Response<TopicProperties>? sourceTopicResponse =
                await administrationClient.GetTopicAsync(sourceName, cancellationToken);

            var sourceTopic = sourceTopicResponse.Value;

            var createTopicOptions =
                new Azure.Messaging.ServiceBus.Administration.CreateTopicOptions(sourceTopic)
                {
                    Name = name
                };

            var response = await administrationClient.CreateTopicAsync(
                createTopicOptions,
                cancellationToken);

            Response<TopicRuntimeProperties> runtimePropertiesResponse =
                await administrationClient.GetTopicRuntimePropertiesAsync(
                    response.Value.Name,
                    cancellationToken);

            return new OperationResult<TopicDetails>(
                true,
                response.Value.ToTopicDetails(runtimePropertiesResponse.Value));
        }
        catch (ServiceBusException ex)
        {
            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
        catch (ArgumentException ex)
        {
            throw new ValidationException(ErrorCodes.InvalidArgument, ex.Message);
        }
    }
    
    public async Task<OperationResult<TopicDetails>> UpdateAsync(
        string connectionName, 
        UpdateTopicOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection = 
                await _connectionManagement.GetAsync(connectionName, cancellationToken);
            
            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connection.ConnectionString);

            var getTopicResponse = await administrationClient.GetTopicAsync(
                options.Name,
                cancellationToken);

            var queueProperties = getTopicResponse.Value;

            var response = await administrationClient.UpdateTopicAsync(
                queueProperties.UpdateFromOptions(options),
                cancellationToken);

            Response<TopicRuntimeProperties> runtimePropertiesResponse =
                await administrationClient.GetTopicRuntimePropertiesAsync(
                    response.Value.Name,
                    cancellationToken);

            return new OperationResult<TopicDetails>(
                true,
                response.Value.ToTopicDetails(runtimePropertiesResponse.Value));
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
    }

    private IList<TopicStructure> GetNestedTopicsList(List<TopicStructure> topicsToNest)
    {
        var nestedTopics = new List<TopicStructure>();

        foreach (TopicStructure topicInfo in topicsToNest)
        {
            var nameParts = topicInfo.Name.Split("/");

            TryAdd(nameParts, nestedTopics);
        }

        return nestedTopics;
    }

    private void TryAdd(string[] nameParts, IList<TopicStructure> topics)
    {
        var currentTopic = topics.FirstOrDefault(p => p.Name == nameParts[0]);

        for (var i = 0; i < nameParts.Length; i++)
        {
            var isFolder = i < nameParts.Length - 1;

            if (currentTopic == null)
            {
                currentTopic = new TopicStructure(
                    nameParts[i],
                    isFolder,
                    !isFolder ? string.Join("/", nameParts) : null,
                    new List<TopicStructure>());
                topics.Add(currentTopic);
            }
            else
            {
                if (currentTopic.Name != nameParts[i])
                {
                    var newTopic = new TopicStructure(nameParts[i],
                        isFolder,
                        !isFolder ? string.Join("/", nameParts) : null,
                        new List<TopicStructure>());

                    currentTopic.ChildTopics.Add(newTopic);

                    currentTopic = newTopic;
                }
            }
        }
    }
}