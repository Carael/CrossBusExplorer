using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Pages;
namespace CrossBusExplorer.Website.ViewModels;

public delegate void SubscriptionOperationEventHandler(
    string connectionName,
    OperationType operationType,
    SubscriptionInfo subscription); 