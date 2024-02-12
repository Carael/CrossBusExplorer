using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Extensions;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Models;
namespace CrossBusExplorer.Website.Mappings;

public static class MessageMappings
{
    public static SendMessage ToSendMessage(this MessageDetailsModel message) =>
        new SendMessage(
            message.Body.TryFormatBody(message.ContentType),
            message.Subject,
            message.To,
            message.ContentType,
            message.CorrelationId,
            message.Id,
            message.PartitionKey,
            message.ReplyTo,
            message.SessionId,
            message.ScheduledEnqueueTime,
            message.TimeToLive,
            message.ApplicationProperties
                .ToDictionary(p => p.Key, p => p.Value.GetApplicationPropertyValue(p.Type)));

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
            ScheduledEnqueueTime =
                message.SystemProperties.ScheduledEnqueueTime == DateTimeOffset.MinValue ? null
                    : message.SystemProperties.ScheduledEnqueueTime,
            TimeToLive = message.SystemProperties.TimeToLive,
            ApplicationProperties =
                new ObservableCollection<KeyValueTypePair>(
                    message.ApplicationProperties?.Select(p =>
                        new KeyValueTypePair
                        {
                            Key = p.Key,
                            Value = p.Value.ToString() ?? string.Empty,
                            Type = p.Value.GetApplicationPropertyType()
                        }).ToList() ?? new List<KeyValueTypePair>())
        };
}
