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
        var connection = await _connectionManagement.GetAsync(connectionName, cancellationToken);
        
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

    public async Task<Result> PurgeAsync(
        string connectionName,
        string name,
        SubQueue subQueue,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection = 
                await _connectionManagement.GetAsync(connectionName, cancellationToken);
            
            await using ServiceBusClient client = new ServiceBusClient(connection.ConnectionString);
            await using var receiver = client.CreateReceiver(name, new ServiceBusReceiverOptions
            {
                SubQueue = (Azure.Messaging.ServiceBus.SubQueue)subQueue,
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
            });

            var totalRemoved = 0;
            var removedCount = 0;
            
            //TODO: async enumerable
            
            do
            {
                removedCount = (await receiver.ReceiveMessagesAsync(
                    1024,
                    TimeSpan.FromSeconds(5),
                    cancellationToken)).Count;
                totalRemoved += removedCount;

            } while (removedCount > 0);

            return new Result(totalRemoved);
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
    }

    public async Task<Result> SendMessagesAsync(
        string connectionString,
        string queueOrTopicName,
        IReadOnlyList<SendMessage> messages,
        CancellationToken cancellationToken)
    {
        ServiceBusMessageBatch? messageBatch = null;

        try
        {
            await using ServiceBusClient client = new ServiceBusClient(connectionString);
            await using var sender = client.CreateSender(queueOrTopicName);

            messageBatch =
                await sender.CreateMessageBatchAsync(cancellationToken);

            var count = 0;

            foreach (ServiceBusMessage serviceBusMessage in
                messages.Select(p => p.ToServiceBusMessage()))
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
                maxMessages??10,
                fromSequenceNumber,
                cancellationToken);
        }

        return await receiver.ReceiveMessagesAsync(
            maxMessages??10,
            TimeSpan.FromSeconds(5),
            cancellationToken);
    }
}