namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record SubscriptionTimeSettings(
    TimeSpan AutoDeleteOnIdle,
    TimeSpan DefaultMessageTimeToLive,
    TimeSpan LockDuration);