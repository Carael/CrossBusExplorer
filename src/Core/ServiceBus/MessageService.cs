using System.Runtime.CompilerServices;
using Azure.Messaging.ServiceBus;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;
using Microsoft.Azure.Amqp.Serialization;
using SubQueue = CrossBusExplorer.ServiceBus.Contracts.Types.SubQueue;
namespace CrossBusExplorer.ServiceBus;

public class MessageService : IMessageService
{
    private const int receiveBatch = 100;
    private const int maxReceiverMessagesCount = 250;
    private readonly IConnectionManagement _connectionManagement;
    private readonly IServiceBusClientFactory _clientFactory;

    public MessageService(
        IConnectionManagement connectionManagement,
        IServiceBusClientFactory clientFactory)
    {
        _connectionManagement = connectionManagement;
        _clientFactory = clientFactory;
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

            ServiceBusClient client = _clientFactory.GetClient(connection);

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

        ServiceBusClient client = _clientFactory.GetClient(connection);
        await using ServiceBusSender sender = client.CreateSender(queueOrTopicName);

        return await SendMessagesInternalAsync(
            sender,
            messages.Select(p => p.ToServiceBusMessage()).ToList(),
            cancellationToken);
    }

    public async IAsyncEnumerable<PurgeResult> PurgeAsync(
        string connectionName,
        string topicOrQueueName,
        string? subscriptionName,
        SubQueue subQueue,
        long totalCount,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connection =
            await _connectionManagement.GetAsync(connectionName, cancellationToken);

        ServiceBusClient client = _clientFactory.GetClient(connection);
        await using var receiver = GetReceiver(
            client,
            topicOrQueueName,
            subscriptionName,
            subQueue,
            ReceiveMode.ReceiveAndDelete);

        var totalRemoved = 0;
        int batchRemovedCount = -1;

        while (totalRemoved <= totalCount && batchRemovedCount > 0 || batchRemovedCount == -1)
        {
            batchRemovedCount = (await receiver.ReceiveMessagesAsync(
                receiveBatch,
                TimeSpan.FromSeconds(5),
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
        long totalCount,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connection =
            await _connectionManagement.GetAsync(connectionName, cancellationToken);

        ServiceBusClient client = _clientFactory.GetClient(connection);
        await using ServiceBusReceiver receiver = GetReceiver(
            client,
            topicOrQueueName,
            subscriptionName,
            subQueue,
            ReceiveMode.ReceiveAndDelete);

        await using ServiceBusSender sender = client.CreateSender(destinationTopicOrQueueName);

        var totalResend = 0;
        int batchResendCount = -1;

        while (totalResend <= totalCount && batchResendCount > 0 || batchResendCount == -1)
        {
            IReadOnlyList<ServiceBusReceivedMessage> messages = await receiver.ReceiveMessagesAsync(
                receiveBatch,
                TimeSpan.FromSeconds(10),
                cancellationToken);

            await SendMessagesInternalAsync(
                sender,
                messages.Select(m => new ServiceBusMessage(m)).ToList(),
                cancellationToken);

            batchResendCount = messages.Count;
            totalResend += batchResendCount;

            yield return new ResendResult(totalResend);
        }

        yield return new ResendResult(totalResend);
    }

    public async Task<Result> DeleteMessage(
        string connectionName,
        string queueOrTopicName,
        string? subscriptionName,
        SubQueue subQueue,
        long sequenceNumber,
        CancellationToken cancellationToken)
    {
        var connection =
            await _connectionManagement.GetAsync(connectionName, cancellationToken);

        ServiceBusClient client = _clientFactory.GetClient(connection);

        await using ServiceBusReceiver receiver =
            GetReceiver(client, queueOrTopicName, subscriptionName, subQueue, ReceiveMode.PeekLock);

        var messageCompleted = false;

        try
        {
            var messagesToUnlock = new List<ServiceBusReceivedMessage>();

            await foreach (ServiceBusReceivedMessage message in receiver.ReceiveMessagesAsync(
                cancellationToken))
            {
                if (message.SequenceNumber > sequenceNumber)
                {
                    messagesToUnlock.Add(message);
                    break;
                }

                if (message.SequenceNumber == sequenceNumber)
                {
                    await receiver.CompleteMessageAsync(message, cancellationToken);

                    messageCompleted = true;
                    break;
                }
                messagesToUnlock.Add(message);
            }

            foreach (ServiceBusReceivedMessage message in messagesToUnlock)
            {
                await receiver.AbandonMessageAsync(message, null, cancellationToken);
            }

        }
        catch (TaskCanceledException)
        {
        }

        return new Result(messageCompleted ? 1 : 0);
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
        var peek = receiver.ReceiveMode == ServiceBusReceiveMode.PeekLock;

        return type switch
        {
            ReceiveType.All => peek
                ? await PeekAsync(receiver, fromSequenceNumber, int.MaxValue, cancellationToken)
                : await ReceiveAsync(receiver, int.MaxValue, cancellationToken),
            ReceiveType.Newest => await PeekNewestAsync(
                receiver,
                fromSequenceNumber,
                maxMessages ?? maxReceiverMessagesCount,
                cancellationToken),
            _ => peek
                ? await PeekAsync(
                    receiver,
                    fromSequenceNumber,
                    maxMessages ?? maxReceiverMessagesCount,
                    cancellationToken)
                : await ReceiveAsync(
                    receiver,
                    maxMessages ?? maxReceiverMessagesCount,
                    cancellationToken)
        };
    }

    private static async Task<IReadOnlyList<ServiceBusReceivedMessage>> PeekAsync(
        ServiceBusReceiver receiver,
        long? fromSequenceNumber,
        int maxMessages,
        CancellationToken cancellationToken)
    {
        var messages = new List<ServiceBusReceivedMessage>();
        var sequenceNumber = fromSequenceNumber;

        while (messages.Count < maxMessages)
        {
            var batch = await receiver.PeekMessagesAsync(
                Math.Min(maxReceiverMessagesCount, maxMessages - messages.Count),
                sequenceNumber,
                cancellationToken);

            // Peek may return fewer than requested even when more messages exist.
            if (batch.Count == 0)
            {
                break;
            }

            messages.AddRange(batch);
            sequenceNumber = batch[^1].SequenceNumber + 1;
        }

        return messages;
    }

    private static async Task<IReadOnlyList<ServiceBusReceivedMessage>> PeekNewestAsync(
        ServiceBusReceiver receiver,
        long? fromSequenceNumber,
        int maxMessages,
        CancellationToken cancellationToken)
    {
        var newest = new Queue<ServiceBusReceivedMessage>(maxMessages);
        var sequenceNumber = fromSequenceNumber;

        while (true)
        {
            var batch = await receiver.PeekMessagesAsync(
                maxReceiverMessagesCount,
                sequenceNumber,
                cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var message in batch)
            {
                if (newest.Count == maxMessages)
                {
                    newest.Dequeue();
                }
                newest.Enqueue(message);
            }

            sequenceNumber = batch[^1].SequenceNumber + 1;
        }

        return newest.Reverse().ToList();
    }

    private static async Task<IReadOnlyList<ServiceBusReceivedMessage>> ReceiveAsync(
        ServiceBusReceiver receiver,
        int maxMessages,
        CancellationToken cancellationToken)
    {
        var messages = new List<ServiceBusReceivedMessage>();

        while (messages.Count < maxMessages)
        {
            var batch = await receiver.ReceiveMessagesAsync(
                Math.Min(maxReceiverMessagesCount, maxMessages - messages.Count),
                TimeSpan.FromSeconds(2),
                cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            messages.AddRange(batch);
        }

        return messages;
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
