using System.Text;
using Azure.Core.Amqp;
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
            ReadBody(receivedMessage),
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

    private static string ReadBody(ServiceBusReceivedMessage receivedMessage)
    {
        AmqpAnnotatedMessage amqpMessage = receivedMessage.GetRawAmqpMessage();
        if (amqpMessage.Body.TryGetValue(out object? value))
        {
            return value switch
            {
                null => string.Empty,
                string stringValue => stringValue,
                _ => throw new NotSupportedException($"Unknown message Body type {value?.GetType().Name}")
            };
        }

        if (amqpMessage.Body.TryGetData(out IEnumerable<ReadOnlyMemory<byte>>? data))
        {
            if (data is null)
            {
                return string.Empty;
            }

            using var memoryStream = new MemoryStream();

            foreach (var segment in data)
            {
                memoryStream.Write(segment.Span);
            }

            memoryStream.Position = 0;
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }

        throw new NotSupportedException("Cannot read the message Body");
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
}