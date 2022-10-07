namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record SendMessage(
    string Body,
    string? Subject,
    string? To,
    string? ContentType,
    string? CorrelationId,
    string? Id,
    string? PartitionKey,
    string? ReplyTo,
    string? SessionId,
    DateTimeOffset? ScheduledEnqueueTime,
    TimeSpan? TimeToLive,
    Dictionary<string, string?>? ApplicationProperties);