using System.Runtime.CompilerServices;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;
using CreateSubscriptionOptions =
    CrossBusExplorer.ServiceBus.Contracts.Types.CreateSubscriptionOptions;
using SubscriptionProperties =
    Azure.Messaging.ServiceBus.Administration.SubscriptionProperties;
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

                SubscriptionRuntimeProperties topicRuntimeProperties =
                    runtimePropertiesResponse.Value;

                yield return subscription.ToSubscriptionInfo(topicRuntimeProperties);
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }
    
    public async Task<SubscriptionDetails> GetAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken)
    {
        var connection = await _connectionManagement.GetAsync(connectionName, cancellationToken);

        ServiceBusAdministrationClient administrationClient =
            new ServiceBusAdministrationClient(connection.ConnectionString);

        var subscriptionResponse =
            await administrationClient.GetSubscriptionAsync(
                topicName,
                subscriptionName,
                cancellationToken);

        var subscription = subscriptionResponse.Value;

        Response<SubscriptionRuntimeProperties> runtimePropertiesResponse =
            await administrationClient.GetSubscriptionRuntimePropertiesAsync(
                topicName,
                subscriptionName,
                cancellationToken);

        return subscription.ToSubscriptionDetails(runtimePropertiesResponse.Value);
    }

    public async Task<OperationResult> DeleteAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection =
                await _connectionManagement.GetAsync(connectionName, cancellationToken);

            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connection.ConnectionString);

            var response = await administrationClient.DeleteSubscriptionAsync(
                topicName,
                subscriptionName,
                cancellationToken);

            return new OperationResult(!response.IsError);
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
    }

    public async Task<OperationResult<SubscriptionDetails>> CreateAsync(
        string connectionName,
        CreateSubscriptionOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection =
                await _connectionManagement.GetAsync(connectionName, cancellationToken);

            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connection.ConnectionString);

            var createSubscriptionOptions = options.MapToCreateSubscriptionOptions();

            var response = await administrationClient.CreateSubscriptionAsync(
                createSubscriptionOptions,
                cancellationToken);

            Response<SubscriptionRuntimeProperties> runtimePropertiesResponse =
                await administrationClient.GetSubscriptionRuntimePropertiesAsync(
                    options.TopicName,
                    options.SubscriptionName,
                    cancellationToken);

            return new OperationResult<SubscriptionDetails>(
                true,
                response.Value.ToSubscriptionDetails(runtimePropertiesResponse.Value));
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

    public async Task<OperationResult<SubscriptionDetails>> CloneAsync(
        string connectionName,
        string subscriptionName,
        string sourceTopicName,
        string sourceSubscriptionName,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection =
                await _connectionManagement.GetAsync(connectionName, cancellationToken);

            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connection.ConnectionString);

            Response<SubscriptionProperties>? sourceSubscriptionResponse =
                await administrationClient.GetSubscriptionAsync(
                    sourceTopicName,
                    sourceSubscriptionName,
                    cancellationToken);

            var sourceSubscription = sourceSubscriptionResponse.Value;

            var createSubscriptionOptions =
                new Azure.Messaging.ServiceBus.Administration.CreateSubscriptionOptions(
                    sourceSubscription)
                {
                    SubscriptionName = subscriptionName
                };

            var response = await administrationClient.CreateSubscriptionAsync(
                createSubscriptionOptions,
                cancellationToken);

            Response<SubscriptionRuntimeProperties> runtimePropertiesResponse =
                await administrationClient.GetSubscriptionRuntimePropertiesAsync(
                    response.Value.TopicName,
                    response.Value.SubscriptionName,
                    cancellationToken);

            return new OperationResult<SubscriptionDetails>(
                true,
                response.Value.ToSubscriptionDetails(runtimePropertiesResponse.Value));
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
    
    public async Task<OperationResult<SubscriptionDetails>> UpdateAsync(
        string connectionName,
        UpdateSubscriptionOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection =
                await _connectionManagement.GetAsync(connectionName, cancellationToken);

            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connection.ConnectionString);

            var subscriptionResponse = await administrationClient.GetSubscriptionAsync(
                options.TopicName,
                options.SubscriptionName,
                cancellationToken);

            var queueProperties = subscriptionResponse.Value;

            var response = await administrationClient.UpdateSubscriptionAsync(
                queueProperties.UpdateFromOptions(options),
                cancellationToken);

            Response<SubscriptionRuntimeProperties> runtimePropertiesResponse =
                await administrationClient.GetSubscriptionRuntimePropertiesAsync(
                    response.Value.TopicName,
                    response.Value.SubscriptionName,
                    cancellationToken);

            return new OperationResult<SubscriptionDetails>(
                true,
                response.Value.ToSubscriptionDetails(runtimePropertiesResponse.Value));
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
    }
}