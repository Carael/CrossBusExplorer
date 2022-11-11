namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record TopicStructure(
    string Name,
    bool IsFolder,
    string? FullName,
    IList<TopicStructure> ChildTopics);