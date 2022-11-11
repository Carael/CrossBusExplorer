using System.Runtime.CompilerServices;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;
using CreateQueueOptions = CrossBusExplorer.ServiceBus.Contracts.Types.CreateQueueOptions;
using QueueProperties = Azure.Messaging.ServiceBus.Administration.QueueProperties;

namespace CrossBusExplorer.ServiceBus;

public class QueueService : IQueueService
{
    private readonly IConnectionManagement _connectionManagement;
    public QueueService(IConnectionManagement connectionManagement)
    {
        _connectionManagement = connectionManagement;
    }
    
    public async IAsyncEnumerable<QueueInfo> GetAsync(
        string connectionName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connection = await _connectionManagement.GetAsync(connectionName, cancellationToken);
        
        ServiceBusAdministrationClient administrationClient =
            new ServiceBusAdministrationClient(connection.ConnectionString);

        AsyncPageable<QueueProperties> queuesPageable =
            administrationClient.GetQueuesAsync(cancellationToken);

        IAsyncEnumerator<QueueProperties> enumerator =
            queuesPageable.GetAsyncEnumerator(cancellationToken);

        try
        {
            while (await enumerator.MoveNextAsync())
            {
                QueueProperties queue = enumerator.Current;
                Response<QueueRuntimeProperties> runtimePropertiesResponse =
                    await administrationClient.GetQueueRuntimePropertiesAsync(
                        queue.Name,
                        cancellationToken);
                
                QueueRuntimeProperties queueRuntimeProperties = runtimePropertiesResponse.Value;

                yield return queue.ToQueueInfo(queueRuntimeProperties);
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }
    
    public async Task<QueueDetails> GetAsync(
        string connectionName,
        string name,
        CancellationToken cancellationToken)
    {
        var connection = await _connectionManagement.GetAsync(connectionName, cancellationToken);
        
        ServiceBusAdministrationClient administrationClient =
            new ServiceBusAdministrationClient(connection.ConnectionString);

        var queueResponse = await administrationClient.GetQueueAsync(name, cancellationToken);

        var queue = queueResponse.Value;

        Response<QueueRuntimeProperties> runtimePropertiesResponse =
            await administrationClient.GetQueueRuntimePropertiesAsync(
                queue.Name,
                cancellationToken);

        return queue.ToQueueDetails(runtimePropertiesResponse.Value);
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

            var response = await administrationClient.DeleteQueueAsync(name, cancellationToken);

            return new OperationResult(!response.IsError);
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
    }
    public async Task<OperationResult<QueueDetails>> CreateAsync(
        string connectionName,
        CreateQueueOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection = 
                await _connectionManagement.GetAsync(connectionName, cancellationToken);
            
            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connection.ConnectionString);

            var createQueueOptions = options.MapToCreateQueueOptions();

            var response = await administrationClient.CreateQueueAsync(
                createQueueOptions,
                cancellationToken);

            Response<QueueRuntimeProperties> runtimePropertiesResponse =
                await administrationClient.GetQueueRuntimePropertiesAsync(
                    response.Value.Name,
                    cancellationToken);

            return new OperationResult<QueueDetails>(
                true,
                response.Value.ToQueueDetails(runtimePropertiesResponse.Value));
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
    public async Task<OperationResult<QueueDetails>> CloneAsync(
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

            Response<QueueProperties>? sourceQueueResponse =
                await administrationClient.GetQueueAsync(sourceName, cancellationToken);

            var sourceQueue = sourceQueueResponse.Value;

            var createQueueOptions =
                new Azure.Messaging.ServiceBus.Administration.CreateQueueOptions(sourceQueue)
                {
                    Name = name
                };

            var response = await administrationClient.CreateQueueAsync(
                createQueueOptions,
                cancellationToken);

            Response<QueueRuntimeProperties> runtimePropertiesResponse =
                await administrationClient.GetQueueRuntimePropertiesAsync(
                    response.Value.Name,
                    cancellationToken);

            return new OperationResult<QueueDetails>(
                true,
                response.Value.ToQueueDetails(runtimePropertiesResponse.Value));
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

    public async Task<OperationResult<QueueDetails>> UpdateAsync(
        string connectionName,
        UpdateQueueOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection = 
                await _connectionManagement.GetAsync(connectionName, cancellationToken);
            
            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connection.ConnectionString);

            var getQueueResponse = await administrationClient.GetQueueAsync(
                options.Name,
                cancellationToken);

            var queueProperties = getQueueResponse.Value;

            var response = await administrationClient.UpdateQueueAsync(
                queueProperties.UpdateFromOptions(options),
                cancellationToken);

            Response<QueueRuntimeProperties> runtimePropertiesResponse =
                await administrationClient.GetQueueRuntimePropertiesAsync(
                    response.Value.Name,
                    cancellationToken);

            return new OperationResult<QueueDetails>(
                true,
                response.Value.ToQueueDetails(runtimePropertiesResponse.Value));
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
    }
}