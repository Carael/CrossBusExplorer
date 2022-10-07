using System.Runtime.CompilerServices;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;
using CreateQueueOptions = CrossBusExplorer.ServiceBus.Contracts.Types.CreateQueueOptions;
using QueueProperties = Azure.Messaging.ServiceBus.Administration.QueueProperties;

namespace CrossBusExplorer.ServiceBus;

public class QueueService : IQueueService
{
    public async IAsyncEnumerable<QueueInfo> GetAsync(
        string connectionString,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ServiceBusAdministrationClient administrationClient =
            new ServiceBusAdministrationClient(connectionString);

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
        string connectionString,
        string name,
        CancellationToken cancellationToken)
    {
        ServiceBusAdministrationClient administrationClient =
            new ServiceBusAdministrationClient(connectionString);

        var queueResponse = await administrationClient.GetQueueAsync(name, cancellationToken);

        var queue = queueResponse.Value;

        Response<QueueRuntimeProperties> runtimePropertiesResponse =
            await administrationClient.GetQueueRuntimePropertiesAsync(
                queue.Name,
                cancellationToken);

        return queue.ToQueueDetails(runtimePropertiesResponse.Value);
    }
    public async Task<OperationResult> DeleteAsync(
        string connectionString,
        string name,
        CancellationToken cancellationToken)
    {
        try
        {
            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connectionString);

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
        string connectionString,
        CreateQueueOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connectionString);

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
        string connectionString,
        string name,
        string sourceName,
        CancellationToken cancellationToken)
    {
        try
        {
            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connectionString);

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
        string connectionString,
        UpdateQueueOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connectionString);

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