using System.Collections;
namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record Message(
    string Id,
    string? Subject,
    string Body,
    MessageSystemProperties SystemProperties,
    IReadOnlyDictionary<string, string?> ApplicationProperties);