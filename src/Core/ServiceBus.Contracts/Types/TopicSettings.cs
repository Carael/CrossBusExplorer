namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record TopicSettings(
    bool EnableBatchedOperations,
    bool EnablePartitioning,
    bool RequiresDuplicateDetection,
    bool SupportOrdering);