using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Pages;
namespace CrossBusExplorer.Website.ViewModels;

public class NavigationViewModel : INavigationViewModel
{
    private readonly IQueueService _queueService;
    private readonly ITopicService _topicService;
    private readonly ISubscriptionService _subscriptionService;

    private ObservableCollection<ConnectionMenuItem> _connectionMenuItems;

    public NavigationViewModel(
        IConnectionsViewModel connectionsViewModel,
        IQueueService queueService,
        ITopicService topicService,
        ISubscriptionService subscriptionService,
        IQueueViewModel queueViewModel,
        ITopicViewModel topicViewModel,
        ISubscriptionViewModel subscriptionViewModel)
    {
        _queueService = queueService;
        _topicService = topicService;
        _subscriptionService = subscriptionService;
        _connectionMenuItems = new ObservableCollection<ConnectionMenuItem>();
        _connectionMenuItems.CollectionChanged += (_, _) => { this.Notify(PropertyChanged); };
        connectionsViewModel.PropertyChanged += ConnectionsViewModelChanged;
        queueViewModel.OnQueueOperation += this.OnQueueOperation;
        topicViewModel.TopicAdded += this.OnTopicAdded;
        topicViewModel.TopicRemoved += this.OnTopicRemoved;
        subscriptionViewModel.OnSubscriptionOperation += this.OnSubscriptionOperation;
    }

    private void ConnectionsViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is IConnectionsViewModel connectionsViewModel)
        {
            RebuildMenuItems(connectionsViewModel.ServiceBusConnections);
        }
    }

    private void RebuildMenuItems(ObservableCollection<ServiceBusConnection> serviceBusConnections)
    {
        MenuItems.RemoveNonExisting(menuItem =>
            !serviceBusConnections.Any(
                p => p.Name.EqualsInvariantIgnoreCase(menuItem.ConnectionName)));

        foreach (ServiceBusConnection serviceBusConnection in serviceBusConnections)
        {
            MenuItems.AddOrReplace(
                p => p.ConnectionName.EqualsInvariantIgnoreCase(serviceBusConnection.Name),
                new ConnectionMenuItem(
                    serviceBusConnection.Name,
                    serviceBusConnection.Folder));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ConnectionMenuItem> MenuItems
    {
        get => _connectionMenuItems;
        private set
        {
            _connectionMenuItems = value;
            _connectionMenuItems.CollectionChanged += (_, _) =>
            {
                this.Notify(PropertyChanged);
            };
            this.Notify(PropertyChanged);
        }
    }

    public async Task LoadTopics(ConnectionMenuItem menuItem, CancellationToken cancellationToken)
    {
        if (!menuItem.TopicsLoaded && !menuItem.LoadingTopics)
        {
            menuItem.LoadingTopics = true;

            await foreach (var topic in _topicService.GetStructureAsync(
                menuItem.ConnectionName,
                cancellationToken))
            {
                menuItem.Topics.Add(
                    new TopicSubscriptionsModel(topic));

                this.Notify(PropertyChanged);
            }

            menuItem.LoadingTopics = false;
            menuItem.TopicsLoaded = true;

            this.Notify(PropertyChanged);
        }
    }

    public async Task LoadQueues(ConnectionMenuItem menuItem, CancellationToken cancellationToken)
    {
        if (!menuItem.QueuesLoaded && !menuItem.LoadingQueues)
        {
            menuItem.LoadingQueues = true;

            await foreach (var queue in _queueService.GetAsync(
                menuItem.ConnectionName,
                cancellationToken))
            {
                menuItem.Queues.Add(queue);
                this.Notify(PropertyChanged);
            }

            menuItem.LoadingQueues = false;
            menuItem.QueuesLoaded = true;
            this.Notify(PropertyChanged);
        }
    }

    public async Task LoadSubscriptionsAsync(
        string connectionName,
        TopicSubscriptionsModel model)
    {
        if (!model.Loaded && !model.IsLoading && !model.Topic.IsFolder)
        {
            model.IsLoading = true;
            this.Notify(PropertyChanged);

            await foreach (var subscription in _subscriptionService.GetAsync(connectionName,
                model.Topic.FullName!, default))
            {
                model.AddSubscription(subscription);

                this.Notify(PropertyChanged);
            }

            model.IsLoading = false;
            model.Loaded = true;
            this.Notify(PropertyChanged);
        }
    }
    public async Task ReloadTopics(ConnectionMenuItem menuItem, CancellationToken cancellationToken)
    {
        menuItem.Topics.Clear();
        menuItem.TopicsLoaded = false;
        await LoadTopics(menuItem, cancellationToken);
    }
    
    public async Task ReloadQueues(ConnectionMenuItem menuItem, CancellationToken cancellationToken)
    {
        menuItem.Queues.Clear();
        menuItem.QueuesLoaded = false;
        await LoadQueues(menuItem, cancellationToken);
    }
    
    public async Task ReloadSubscriptions(string connectionName, TopicSubscriptionsModel model)
    {
        model.Subscriptions.Clear();
        model.Loaded = false;
        await LoadSubscriptionsAsync(connectionName, model);
    }

    private void OnQueueOperation(string connectionName, OperationType operationType,
        QueueInfo queueInfo)
    {
        var menuItem =
            MenuItems.First(p => p.ConnectionName.EqualsInvariantIgnoreCase(connectionName));

        switch (operationType)
        {

            case OperationType.Create:
                AddQueue(menuItem, queueInfo);
                break;
            case OperationType.Update:
                UpdateQueue(menuItem, queueInfo);
                break;
            case OperationType.Delete:
                DeleteQueue(menuItem, queueInfo);
                break;
            default:
                throw new NotSupportedException($"Operation {operationType} is not supported.");
        }
    }

    private void UpdateQueue(ConnectionMenuItem menuItem, QueueInfo queueInfo)
    {
        menuItem.Queues.AddOrReplace(p => p.Name.EqualsInvariantIgnoreCase(queueInfo.Name),
            queueInfo);

        this.Notify(PropertyChanged);
    }

    private void AddQueue(ConnectionMenuItem menuItem, QueueInfo queueInfo)
    {
        menuItem.Queues.Add(queueInfo);

        this.Notify(PropertyChanged);
    }

    private void DeleteQueue(ConnectionMenuItem menuItem, QueueInfo queueInfo)
    {
        menuItem.Queues.Remove(queueInfo);

        this.Notify(PropertyChanged);
    }

    private void OnTopicRemoved(string connectionName, string topicName)
    {
        var menuItem =
            MenuItems.First(p => p.ConnectionName.EqualsInvariantIgnoreCase(connectionName));

        for (int i = 0; i < menuItem.Topics.Count; i++)
        {
            var current = menuItem.Topics[i];

            if (current.Topic.FullName != null &&
                current.Topic.FullName.EqualsInvariantIgnoreCase(topicName))
            {
                menuItem.Topics.Remove(current);
                break;
            }

            var isNested = TryGetTopicSubscriptionModel(current, topicName);

            if (isNested)
            {
                menuItem.Topics.Remove(current);
                break;
            }
        }

        this.Notify(PropertyChanged);
    }

    private bool TryGetTopicSubscriptionModel(
        TopicSubscriptionsModel topicSubscriptionsModel,
        string topicName)
    {
        for (var i = 0; i < topicSubscriptionsModel.ChildrenModels.Count; i++)
        {
            TopicSubscriptionsModel current = topicSubscriptionsModel.ChildrenModels[i];

            if (current.Topic.FullName != null &&
                current.Topic.FullName.EqualsInvariantIgnoreCase(topicName))
            {
                topicSubscriptionsModel.ChildrenModels.Remove(current);

                return true;
            }

            return TryGetTopicSubscriptionModel(current, topicName);
        }

        return false;
    }

    private void TryRemoveSubscription(
        TopicSubscriptionsModel topicSubscriptionsModel,
        string topicName,
        string subscriptionName)
    {
        for (var i = 0; i < topicSubscriptionsModel.ChildrenModels.Count; i++)
        {
            TopicSubscriptionsModel current = topicSubscriptionsModel.ChildrenModels[i];

            if (current.Topic.FullName != null &&
                current.Topic.FullName.EqualsInvariantIgnoreCase(topicName))
            {
                var subscription =
                    current.Subscriptions
                        .FirstOrDefault(p => p.SubscriptionName
                            .Equals(subscriptionName, StringComparison.InvariantCultureIgnoreCase));

                current.Subscriptions.Remove(subscription);

                break;
            }

            TryRemoveSubscription(current, topicName, subscriptionName);
        }
    }


    private void OnTopicAdded(string connectionName, TopicInfo topicInfo)
    {
        var menuItem =
            MenuItems.First(p => p.ConnectionName.EqualsInvariantIgnoreCase(connectionName));

        menuItem.Topics.Add(
            new TopicSubscriptionsModel(
                new TopicStructure(
                    topicInfo.Name,
                    false,
                    topicInfo.Name,
                    new List<TopicStructure>())));

        this.Notify(PropertyChanged);
    }

    private void OnSubscriptionOperation(
        string connectionName,
        OperationType operationType,
        SubscriptionInfo subscription)
    {
        ConnectionMenuItem menuItem =
            MenuItems.First(p => p.ConnectionName.EqualsInvariantIgnoreCase(connectionName));

        switch (operationType)
        {

            case OperationType.Create:
            case OperationType.Update:
                AddOrReplaceSubscription(menuItem, subscription);
                break;
            case OperationType.Delete:
                RemoveSubscription(
                    connectionName,
                    subscription.TopicName,
                    subscription.SubscriptionName);
                break;
            default:
                throw new NotSupportedException($"Operation {operationType} is not supported.");
        }

    }

    private void AddOrReplaceSubscription(ConnectionMenuItem menuItem,
        SubscriptionInfo subscription)
    {
        for (int i = 0; i < menuItem.Topics.Count; i++)
        {
            var current = menuItem.Topics[i];

            if (current.Topic.FullName != null &&
                current.Topic.FullName.EqualsInvariantIgnoreCase(subscription.TopicName))
            {
                current.Subscriptions.AddOrReplace(
                    p => p.SubscriptionName.EqualsInvariantIgnoreCase(
                        subscription
                            .SubscriptionName),
                    subscription);
                break;
            }

            TryAddSubscription(current, subscription);
        }

        this.Notify(PropertyChanged);
    }

    private void RemoveSubscription(
        string connectionName,
        string topicName,
        string subscriptionName)
    {
        var menuItem =
            MenuItems.First(p => p.ConnectionName.EqualsInvariantIgnoreCase(connectionName));

        for (int i = 0; i < menuItem.Topics.Count; i++)
        {
            var current = menuItem.Topics[i];

            if (current.Topic.FullName != null &&
                current.Topic.FullName.EqualsInvariantIgnoreCase(topicName))
            {
                var subscription =
                    current.Subscriptions
                        .FirstOrDefault(p => p.SubscriptionName
                            .Equals(subscriptionName, StringComparison.InvariantCultureIgnoreCase));

                current.Subscriptions.Remove(subscription);
                break;
            }

            TryRemoveSubscription(current, topicName, subscriptionName);
        }

        this.Notify(PropertyChanged);
    }

    private void TryAddSubscription(
        TopicSubscriptionsModel topicSubscriptionsModel,
        SubscriptionInfo subscription)
    {
        for (var i = 0; i < topicSubscriptionsModel.ChildrenModels.Count; i++)
        {
            TopicSubscriptionsModel current = topicSubscriptionsModel.ChildrenModels[i];

            if (current.Topic.FullName != null &&
                current.Topic.FullName.EqualsInvariantIgnoreCase(subscription.TopicName))
            {
                current.Subscriptions.AddOrReplace(
                    p => p.SubscriptionName
                        .EqualsInvariantIgnoreCase(subscription.SubscriptionName),
                    subscription);

                break;
            }

            TryAddSubscription(current, subscription);
        }
    }
}