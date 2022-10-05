using System.Text;
using Azure.Messaging.ServiceBus;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus;

public class MessageService : IMessageService
{
    public async Task<IReadOnlyList<Message>> GetMessagesAsync(
        string connectionString,
        string queueName,
        int messagesCount,
        ReceiveMode receiveMode,
        long? fromSequenceNumber,
        CancellationToken cancellationToken)
    {
        await using ServiceBusClient client = new ServiceBusClient(connectionString);

        var receiver = client.CreateReceiver(queueName, new ServiceBusReceiverOptions
        {
            ReceiveMode = Enum.Parse<ServiceBusReceiveMode>(receiveMode.ToString())
        });

        IReadOnlyList<ServiceBusReceivedMessage>? result =
            await ReceiveMessagesAsync(
                receiver, 
                messagesCount,
                fromSequenceNumber,
                cancellationToken);

        return result?.Select(MapToMessage).ToList() ?? new List<Message>();
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
            TimeSpan.FromSeconds(5),
            cancellationToken);
    }
    
    private Message MapToMessage(ServiceBusReceivedMessage receivedMessage)
    {
        return new Message(
            receivedMessage.MessageId,
            receivedMessage.Subject,
            Encoding.UTF8.GetString(receivedMessage.Body),
            new MessageSystemProperties(
                receivedMessage.ContentType,
                receivedMessage.CorrelationId,
                receivedMessage.DeadLetterSource,
                receivedMessage.DeadLetterReason,
                receivedMessage.DeadLetterErrorDescription,
                receivedMessage.DeliveryCount,
                receivedMessage.EnqueuedSequenceNumber,
                receivedMessage.EnqueuedTime,
                receivedMessage.ExpiresAt,
                receivedMessage.LockedUntil,
                receivedMessage.LockToken,
                receivedMessage.PartitionKey,
                receivedMessage.TransactionPartitionKey,
                receivedMessage.ReplyTo,
                receivedMessage.ReplyToSessionId,
                receivedMessage.ScheduledEnqueueTime,
                receivedMessage.SequenceNumber,
                receivedMessage.SessionId,
                (MessageState)receivedMessage.State,
                receivedMessage.TimeToLive,
                receivedMessage.To
            ),
            receivedMessage.ApplicationProperties.ToDictionary(p=>p.Key, p=>p.Value?.ToString()));
    }
}