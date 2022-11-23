using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Website.ViewModels;

public delegate void SubscriptionAddedEventHandler(
    string connectionName,
    SubscriptionInfo subscription); 