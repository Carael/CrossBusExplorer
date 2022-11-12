namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record TopicDetails(
    TopicInfo Info,
    TopicSettings Settings,
    TopicTimeSettings TimeSettings,
    TopicProperties Properties);