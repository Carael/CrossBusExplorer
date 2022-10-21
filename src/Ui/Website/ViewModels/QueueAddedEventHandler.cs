using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Website.ViewModels;

public delegate void QueueAddedEventHandler(string connectionName, QueueInfo queueInfo); 