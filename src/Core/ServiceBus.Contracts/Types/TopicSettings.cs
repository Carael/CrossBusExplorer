namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record TopicSettings(
    bool EnableBatchedOperations,
    bool EnableDeadLetteringOnMessageExpiration,
    bool EnablePartitioning,
    bool RequiresDuplicateDetection,
    bool RequiresSession);