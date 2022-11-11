namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record TopicInfo(
    string Name,
    ServiceBusEntityStatus Status,
    long SizeInBytes,
    DateTimeOffset CreatedAt,
    DateTimeOffset AccessedAt,
    DateTimeOffset UpdatedAt,
    long ScheduledMessagesCount)
{
    public long SizeInMB => SizeInBytes / 1024 / 1024;
}