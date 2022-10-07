namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record QueueTimeSettings(
    TimeSpan AutoDeleteOnIdle,
    TimeSpan DefaultMessageTimeToLive,
    TimeSpan DuplicateDetectionHistoryTimeWindow,
    TimeSpan LockDuration);