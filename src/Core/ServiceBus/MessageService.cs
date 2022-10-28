using System.Runtime.CompilerServices;
using Azure.Messaging.ServiceBus;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;
using SubQueue = CrossBusExplorer.ServiceBus.Contracts.Types.SubQueue;
namespace CrossBusExplorer.ServiceBus;

public class MessageService : IMessageService
{
    private const int receiveBatch = 100;
    private const int maxReceiverMessagesCount = 250;
    private readonly IConnectionManagement _connectionManagement;
    public MessageService(IConnectionManagement connectionManagement)
    {
        _connectionManagement = connectionManagement;
    }

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(
        string connectionName,
        string queueOrTopicName,
        string? subscriptionName,
        SubQueue subQueue,
        ReceiveMode mode,
        ReceiveType type,
        int? messagesCount,
        long? fromSequenceNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection =
                await _connectionManagement.GetAsync(connectionName, cancellationToken);

            await using ServiceBusClient client = new ServiceBusClient(connection.ConnectionString);

            await using var receiver =
                GetReceiver(client, queueOrTopicName, subscriptionName, subQueue, mode);

            IReadOnlyList<ServiceBusReceivedMessage>? result =
                await ReceiveMessagesAsync(
                    receiver,
                    type,
                    messagesCount,
                    fromSequenceNumber,
                    cancellationToken);

            return result?.Select(p => p.MapToMessage()).ToList() ?? new List<Message>();
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ErrorCodes.InvalidOperation, ex.Message);
        }
    }

    public async Task<Result> SendMessagesAsync(
        string connectionName,
        string queueOrTopicName,
        IReadOnlyList<SendMessage> messages,
        CancellationToken cancellationToken)
    {
        var connection =
            await _connectionManagement.GetAsync(connectionName, cancellationToken);

        await using var client = new ServiceBusClient(connection.ConnectionString);
        await using ServiceBusSender sender = client.CreateSender(queueOrTopicName);

        return await SendMessagesInternalAsync(
            sender, 
            messages.Select(p=>p.ToServiceBusMessage()).ToList(), 
            cancellationToken);
    }
    
    public async IAsyncEnumerable<PurgeResult> PurgeAsync(
        string connectionName,
        string topicOrQueueName,
        string? subscriptionName,
        SubQueue subQueue,
        CancellationToken cancellationToken)
    {
        var connection =
            await _connectionManagement.GetAsync(connectionName, cancellationToken);

        await using ServiceBusClient client = new ServiceBusClient(connection.ConnectionString);
        await using var receiver = GetReceiver(
            client,
            topicOrQueueName,
            subscriptionName,
            subQueue,
            ReceiveMode.ReceiveAndDelete);

        var totalRemoved = 0;
        int batchRemovedCount = -1;

        while (batchRemovedCount > 0 || batchRemovedCount == -1)
        {
            batchRemovedCount = (await receiver.ReceiveMessagesAsync(
                receiveBatch,
                TimeSpan.FromSeconds(10),
                cancellationToken)).Count;
            totalRemoved += batchRemovedCount;

            yield return new PurgeResult(totalRemoved);
        }

        yield return new PurgeResult(totalRemoved);
    }
    public async IAsyncEnumerable<ResendResult> ResendAsync(
        string connectionName, 
        string topicOrQueueName,
        string? subscriptionName,
        SubQueue subQueue, 
        string destinationTopicOrQueueName, 
        CancellationToken cancellationToken)
    {
        var connection =
            await _connectionManagement.GetAsync(connectionName, cancellationToken);

        await using ServiceBusClient client = new ServiceBusClient(connection.ConnectionString);
        await using ServiceBusReceiver receiver = GetReceiver(
            client,
            topicOrQueueName,
            subscriptionName,
            subQueue,
            ReceiveMode.ReceiveAndDelete);
        
        await using ServiceBusSender sender = client.CreateSender(destinationTopicOrQueueName);

        var totalResend = 0;
        int batchResendCount = -1;

        while (batchResendCount > 0 || batchResendCount == -1)
        {
            IReadOnlyList<ServiceBusReceivedMessage> messages = await receiver.ReceiveMessagesAsync(
                receiveBatch,
                TimeSpan.FromSeconds(10),
                cancellationToken);

            await SendMessagesInternalAsync(
                sender, 
                messages.Select(p => p.MapToServiceBusMessage()).ToList(), 
                cancellationToken);

            batchResendCount = messages.Count;
            totalResend += batchResendCount;

            yield return new ResendResult(totalResend);
        }

        yield return new ResendResult(totalResend);
    }

    private ServiceBusReceiver GetReceiver(
        ServiceBusClient client,
        string queueOrTopicName,
        string? subscriptionName,
        SubQueue subQueue,
        ReceiveMode receiveMode)
    {
        var receiverOptions = new ServiceBusReceiverOptions
        {
            ReceiveMode = Enum.Parse<ServiceBusReceiveMode>(receiveMode.ToString()),
            SubQueue = Enum.Parse<Azure.Messaging.ServiceBus.SubQueue>(subQueue.ToString()),
        };

        if (subscriptionName != null)
        {
            return client.CreateReceiver(
                queueOrTopicName,
                subscriptionName,
                receiverOptions);
        }

        return client.CreateReceiver(queueOrTopicName, receiverOptions);
    }

    private async Task<IReadOnlyList<ServiceBusReceivedMessage>?> ReceiveMessagesAsync(
        ServiceBusReceiver receiver,
        ReceiveType type,
        int? maxMessages,
        long? fromSequenceNumber,
        CancellationToken cancellationToken)
    {
        if (receiver.ReceiveMode == ServiceBusReceiveMode.PeekLock)
        {
            return await receiver.PeekMessagesAsync(
                maxMessages ?? maxReceiverMessagesCount,
                fromSequenceNumber,
                cancellationToken);
        }

        return await receiver.ReceiveMessagesAsync(
            maxMessages ?? maxReceiverMessagesCount,
            null,
            cancellationToken: cancellationToken);
    }

    private async Task<Result> SendMessagesInternalAsync(
        ServiceBusSender sender,
        IReadOnlyList<ServiceBusMessage> messages,
        CancellationToken cancellationToken)
    {
        ServiceBusMessageBatch? messageBatch = null;

        try
        {
            messageBatch =
                await sender.CreateMessageBatchAsync(cancellationToken);

            var count = 0;

            foreach (ServiceBusMessage serviceBusMessage in messages)
            {
                if (!messageBatch.TryAddMessage(serviceBusMessage))
                {
                    if (messageBatch.Count == 0)
                    {
                        throw new MessageTooBigException();
                    }

                    await sender.SendMessagesAsync(messageBatch, cancellationToken);
                    messageBatch.Dispose();

                    messageBatch = await sender.CreateMessageBatchAsync(cancellationToken);
                    messageBatch.TryAddMessage(serviceBusMessage);
                }

                count++;
            }

            if (messageBatch.Count > 0)
            {
                await sender.SendMessagesAsync(messageBatch, cancellationToken);
            }

            return new Result(count);
        }
        catch (ArgumentException ex)
        {
            //TODO: log

            throw new ValidationException(ErrorCodes.InvalidArgument, ex.Message);
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
        finally
        {
            messageBatch?.Dispose();
        }
    }
}