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
            receivedMessage.ApplicationProperties.ToDictionary(p => p.Key,
                p => p.Value?.ToString()));
    }

    public static ServiceBusMessage ToServiceBusMessage(this SendMessage message)
    {
        var sbMessage = new ServiceBusMessage(message.Body);

        if (!string.IsNullOrEmpty(message.Subject))
        {
            sbMessage.Subject = message.Subject;
        }

        if (message.ApplicationProperties != null)
        {
            foreach (var applicationProperty in message.ApplicationProperties)
            {
                sbMessage.ApplicationProperties.Add(
                    applicationProperty.Key,
                    applicationProperty.Value);
            }
        }

        if (!string.IsNullOrEmpty(message.To))
        {
            sbMessage.To = message.To;
        }

        if (!string.IsNullOrEmpty(message.ContentType))
        {
            sbMessage.ContentType = message.ContentType;
        }

        if (!string.IsNullOrEmpty(message.CorrelationId))
        {
            sbMessage.CorrelationId = message.CorrelationId;
        }

        if (!string.IsNullOrEmpty(message.Id))
        {
            sbMessage.MessageId = message.Id;
        }

        if (!string.IsNullOrEmpty(message.PartitionKey))
        {
            sbMessage.PartitionKey = message.PartitionKey;
        }

        if (!string.IsNullOrEmpty(message.ReplyTo))
        {
            sbMessage.ReplyTo = message.ReplyTo;
        }

        if (!string.IsNullOrEmpty(message.SessionId))
        {
            sbMessage.SessionId = message.SessionId;
        }

        if (message.ScheduledEnqueueTime != null)
        {
            sbMessage.ScheduledEnqueueTime = message.ScheduledEnqueueTime.Value;
        }

        if (message.TimeToLive != null)
        {
            sbMessage.TimeToLive = message.TimeToLive.Value;
        }

        return sbMessage;
    }

    public static ServiceBusMessage MapToServiceBusMessage(
        this ServiceBusReceivedMessage receivedMessage)
    {
        var sbMessage = new ServiceBusMessage(receivedMessage.Body);

        if (!string.IsNullOrEmpty(receivedMessage.Subject))
        {
            sbMessage.Subject = receivedMessage.Subject;
        }

        if (receivedMessage.ApplicationProperties != null)
        {
            foreach (var applicationProperty in receivedMessage.ApplicationProperties)
            {
                sbMessage.ApplicationProperties.Add(
                    applicationProperty.Key,
                    applicationProperty.Value);
            }
        }

        if (!string.IsNullOrEmpty(receivedMessage.To))
        {
            sbMessage.To = receivedMessage.To;
        }

        if (!string.IsNullOrEmpty(receivedMessage.ContentType))
        {
            sbMessage.ContentType = receivedMessage.ContentType;
        }

        if (!string.IsNullOrEmpty(receivedMessage.CorrelationId))
        {
            sbMessage.CorrelationId = receivedMessage.CorrelationId;
        }

        if (!string.IsNullOrEmpty(receivedMessage.MessageId))
        {
            sbMessage.MessageId = receivedMessage.MessageId;
        }

        if (!string.IsNullOrEmpty(receivedMessage.PartitionKey))
        {
            sbMessage.PartitionKey = receivedMessage.PartitionKey;
        }

        if (!string.IsNullOrEmpty(receivedMessage.ReplyTo))
        {
            sbMessage.ReplyTo = receivedMessage.ReplyTo;
        }

        if (!string.IsNullOrEmpty(receivedMessage.SessionId))
        {
            sbMessage.SessionId = receivedMessage.SessionId;
        }

        if (receivedMessage.ScheduledEnqueueTime != null &&
            receivedMessage.ScheduledEnqueueTime != DateTimeOffset.MinValue)
        {
            sbMessage.ScheduledEnqueueTime = receivedMessage.ScheduledEnqueueTime;
        }

        if (receivedMessage.TimeToLive != null &&
            receivedMessage.ScheduledEnqueueTime != DateTimeOffset.MinValue)
        {
            sbMessage.TimeToLive = receivedMessage.TimeToLive;
        }

        return sbMessage;
    }
}