using System.Text;
using Azure.Messaging.ServiceBus;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus.Mappings;

public static class MessageMappings
{
    public static Message MapToMessage(this ServiceBusReceivedMessage receivedMessage)
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