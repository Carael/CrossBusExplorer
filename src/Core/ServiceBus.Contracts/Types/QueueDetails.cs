namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record QueueDetails(
    QueueInfo Info,
    QueueSettings Settings,
    TimeSpan AutoDeleteOnIdle,
    TimeSpan DefaultMessageTimeToLive,
    TimeSpan DuplicateDetectionHistoryTimeWindow,
    TimeSpan LockDuration,
    long MaxSizeInMegabytes,
    int MaxDeliveryCount,
    string UserMetadata,
    string ForwardTo,
    string ForwardDeadLetteredMessagesTo
);