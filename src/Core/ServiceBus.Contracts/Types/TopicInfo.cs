namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record TopicInfo(
    string Name,
    bool IsFolder,
    string? FullName,
    IList<TopicInfo> ChildTopics);