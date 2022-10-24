using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Website.Models;

public record RequeueMessage(
    string QueueOrTopicName,
    Message Message);