using System.Linq;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Models;
namespace CrossBusExplorer.Website.Mappings;

public static class MessageMappings
{
    public static SendMessage ToSendMessage(this Message message) =>
        new SendMessage(
            message.Body.TryFormatBody(message.SystemProperties.ContentType),
            message.Subject,
            message.SystemProperties.To,
            message.SystemProperties.ContentType,
            message.SystemProperties.CorrelationId,
            message.Id,
            message.SystemProperties.PartitionKey,
            message.SystemProperties.ReplyTo,
            message.SystemProperties.SessionId,
            message.SystemProperties.ScheduledEnqueueTime,
            message.SystemProperties.TimeToLive,
            message.ApplicationProperties?.ToDictionary(p => p.Key, p => p.Value));
    
    public static MessageDetailsModel ToSendMessageModel(this Message message) =>
        new MessageDetailsModel
        {
            Body = message.Body.TryFormatBody(message.SystemProperties.ContentType),
            Subject = message.Subject,
            To = message.SystemProperties.To,
            ContentType = message.SystemProperties.ContentType,
            CorrelationId = message.SystemProperties.CorrelationId,
            Id = message.Id,
            PartitionKey = message.SystemProperties.PartitionKey,
            ReplyTo = message.SystemProperties.ReplyTo,
            SessionId = message.SystemProperties.SessionId,
            ScheduledEnqueueTime = message.SystemProperties.ScheduledEnqueueTime,
            TimeToLive = message.SystemProperties.TimeToLive,
            ApplicationProperties =
                message.ApplicationProperties?.ToDictionary(p => p.Key, p => p.Value)
        };
}