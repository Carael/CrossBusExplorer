namespace CrossBusExplorer.Website.ViewModels;

public delegate void SubscriptionRemovedEventHandler(
    string connectionName,
    string topicName,
    string subscriptionName); 