using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Website.ViewModels;

public delegate void TopicAddedEventHandler(string connectionName, TopicInfo topicInfo); 