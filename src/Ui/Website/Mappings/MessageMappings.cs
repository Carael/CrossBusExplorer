using System.Linq;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Website.Mappings;

public static class MessageMappings
{
    public static SendMessage ToSendMessage(this Message message) =>
        new SendMessage(
            message.Body,
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
}