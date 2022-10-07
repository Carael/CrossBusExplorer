using System.Runtime.CompilerServices;
using Azure.Messaging.ServiceBus;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;
using SubQueue = CrossBusExplorer.ServiceBus.Contracts.Types.SubQueue;
namespace CrossBusExplorer.ServiceBus;

public class MessageService : IMessageService
{
    public async Task<IReadOnlyList<Message>> GetMessagesAsync(
        string connectionString,
        string queueOrTopicName,
        string? subscriptionName,
        int messagesCount,
        ReceiveMode receiveMode,
        long? fromSequenceNumber,
        CancellationToken cancellationToken)
    {
        await using ServiceBusClient client = new ServiceBusClient(connectionString);

        await using var receiver =
            GetReceiver(client, queueOrTopicName, subscriptionName, receiveMode);

        IReadOnlyList<ServiceBusReceivedMessage>? result =
            await ReceiveMessagesAsync(
                receiver,
                messagesCount,
                fromSequenceNumber,
                cancellationToken);

        return result?.Select(p => p.MapToMessage()).ToList() ?? new List<Message>();
    }
    private ServiceBusReceiver GetReceiver(
        ServiceBusClient client,
        string queueOrTopicName,
        string? subscriptionName,
        ReceiveMode receiveMode)
    {
        var receiverOptions = new ServiceBusReceiverOptions
        {
            ReceiveMode = Enum.Parse<ServiceBusReceiveMode>(receiveMode.ToString())
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

    public async Task<Removed> PurgeAsync(
        string connectionString,
        string name,
        SubQueue subQueue,
        CancellationToken cancellationToken)
    {
        try
        {
            await using ServiceBusClient client = new ServiceBusClient(connectionString);
            await using var receiver = client.CreateReceiver(name, new ServiceBusReceiverOptions
            {
                SubQueue = (Azure.Messaging.ServiceBus.SubQueue)subQueue,
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
            });

            var totalRemoved = 0;
            var removedCount = 0;

            do
            {
                removedCount = (await receiver.ReceiveMessagesAsync(
                    1024,
                    TimeSpan.FromSeconds(15),
                    cancellationToken)).Count;
                totalRemoved += removedCount;

            } while (removedCount > 0);

            return new Removed(totalRemoved);
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
    }

    public async IAsyncEnumerable<Removed> PurgeAsyncEnumerable(
        string connectionString,
        string name,
        SubQueue subQueue,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using ServiceBusClient client = new ServiceBusClient(connectionString);
        await using var receiver = client.CreateReceiver(name, new ServiceBusReceiverOptions
        {
            SubQueue = (Azure.Messaging.ServiceBus.SubQueue)subQueue,
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
        });

        var totalRemoved = 0;
        var removedCount = 0;

        do
        {
            removedCount = (await receiver.ReceiveMessagesAsync(
                1024,
                TimeSpan.FromSeconds(10),
                cancellationToken)).Count;
            totalRemoved += removedCount;

            yield return new Removed(totalRemoved);
        } while (removedCount > 0);
    }

    public async Task<Sent> SendMessagesAsync(
        string connectionString,
        string queueOrTopicName,
        IReadOnlyList<SendMessage> messages,
        CancellationToken cancellationToken)
    {
        await using ServiceBusClient client = new ServiceBusClient(connectionString);
        await using var sender = client.CreateSender(queueOrTopicName);

        ServiceBusMessageBatch messageBatch =
            await sender.CreateMessageBatchAsync(cancellationToken);

        var count = 0;

        try
        {
            foreach (ServiceBusMessage serviceBusMessage in
                messages.Select(CreateServiceBusMessage))
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
        }
        finally
        {
            messageBatch.Dispose();
        }

        return new Sent(count);
    }

    private ServiceBusMessage CreateServiceBusMessage(SendMessage sendMessage)
    {
        var message = new ServiceBusMessage(sendMessage.Body);

        return message;
    }

    private async Task<IReadOnlyList<ServiceBusReceivedMessage>?> ReceiveMessagesAsync(
        ServiceBusReceiver receiver,
        int maxMessages,
        long? fromSequenceNumber,
        CancellationToken cancellationToken)
    {
        if (receiver.ReceiveMode == ServiceBusReceiveMode.PeekLock)
        {
            return await receiver.PeekMessagesAsync(
                maxMessages,
                fromSequenceNumber,
                cancellationToken);
        }

        return await receiver.ReceiveMessagesAsync(
            maxMessages,
            TimeSpan.FromSeconds(10),
            cancellationToken);
    }
}