namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record SendMessage(
    string Body,
    string? Subject,
    MessageSystemProperties? SystemProperties,
    IReadOnlyDictionary<string, string?>? ApplicationProperties);