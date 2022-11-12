namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record QueueInfo(
    string Name,
    ServiceBusEntityStatus Status,
    long SizeInBytes,
    DateTimeOffset CreatedAt,
    DateTimeOffset AccessedAt,
    DateTimeOffset UpdatedAt,
    long ActiveMessagesCount,
    long DeadLetterMessagesCount,
    long ScheduledMessagesCount,
    long InTransferMessagesCount,
    long TransferDeadLetterMessagesCount,
    long TotalMessagesCount)
{
    public long SizeInMB => SizeInBytes / 1024 / 1024;
}