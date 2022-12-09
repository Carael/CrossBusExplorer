using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Pages;
namespace CrossBusExplorer.Website.ViewModels;

public delegate void QueueOperationEventHandler(
    string connectionName,
    OperationType operationType, 
    QueueInfo queueInfo); 