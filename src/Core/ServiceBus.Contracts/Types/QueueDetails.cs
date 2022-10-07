namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record QueueDetails(
    QueueInfo Info,
    QueueSettings Settings,
    QueueTimeSettings TimeSettings,
    QueueProperties Properties);