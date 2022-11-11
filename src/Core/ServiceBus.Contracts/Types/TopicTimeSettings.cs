namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record TopicTimeSettings(
    TimeSpan AutoDeleteOnIdle,
    TimeSpan DefaultMessageTimeToLive,
    TimeSpan DuplicateDetectionHistoryTimeWindow,
    TimeSpan LockDuration);