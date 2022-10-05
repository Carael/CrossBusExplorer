namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record QueueSettings(
    bool EnableBatchedOperations,
    bool EnableDeadLetteringOnMessageExpiration,
    bool EnablePartitioning,
    bool RequiresDuplicateDetection,
    bool RequiresSession);