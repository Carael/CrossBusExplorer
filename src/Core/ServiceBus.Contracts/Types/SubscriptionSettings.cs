namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record SubscriptionSettings(
    bool EnableBatchedOperations,
    bool EnableDeadLetteringOnMessageExpiration,
    bool RequiresSession,
    bool EnableDeadLetteringOnFilterEvaluationExceptions);