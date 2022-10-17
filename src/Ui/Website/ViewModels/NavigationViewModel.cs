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
        IQueueViewModel queueViewModel)
    {
        connectionsViewModel.PropertyChanged += ConnectionsViewModelChanged;
        _queueService = queueService;
        _topicService = topicService;
        _subscriptionService = subscriptionService;
        _connectionMenuItems = new ObservableCollection<ConnectionMenuItem>();
        _connectionMenuItems.CollectionChanged += (_, _) => { this.Notify(PropertyChanged); };
        queueViewModel.QueueAdded += this.OnQueueAdded;
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
                new ConnectionMenuItem(serviceBusConnection.Name));
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

            await foreach (var topic in _topicService.GetAsync(
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

    private void OnQueueAdded(string connectionName, QueueInfo queueinfo)
    {
        var menuItem =
            MenuItems.First(p => p.ConnectionName.EqualsInvariantIgnoreCase(connectionName));
        
        menuItem.Queues.Add(queueinfo);
        this.Notify(PropertyChanged);
    }
}