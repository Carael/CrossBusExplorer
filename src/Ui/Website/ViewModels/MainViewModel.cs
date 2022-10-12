using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Models;
namespace CrossBusExplorer.Website.ViewModels;

public class MainViewModel : IMainViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly IConnectionManagement _connectionManagement;
    private readonly IQueueService _queueService;
    private readonly ITopicService _topicService;

    private ObservableCollection<ServiceBusConnection> _serviceBusConnections =
        new ObservableCollection<ServiceBusConnection>();
    private ObservableCollection<ConnectionMenuItem> _connectionMenuItems =
        new ObservableCollection<ConnectionMenuItem>();

    public MainViewModel(
        IConnectionManagement connectionManagement,
        IQueueService queueService,
        ITopicService topicService)
    {
        _connectionManagement = connectionManagement;
        _queueService = queueService;
        _topicService = topicService;
    }

    public ObservableCollection<ServiceBusConnection> ServiceBusConnections
    {
        get => _serviceBusConnections;
        private set
        {
            _serviceBusConnections = value;
            _serviceBusConnections.CollectionChanged += (_, _) =>
            {
                this.Notify(PropertyChanged);
            };
            this.Notify(PropertyChanged);
        }
    }
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

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        ServiceBusConnections =
            new ObservableCollection<ServiceBusConnection>(
                await _connectionManagement.GetAsync(default));

        MenuItems = new ObservableCollection<ConnectionMenuItem>(
            ServiceBusConnections.Select(p => new ConnectionMenuItem(p.Name)));
    }

    public async Task SaveConnectionAsync(
        string name,
        string connectionString,
        CancellationToken cancellationToken)
    {
        ServiceBusConnection newConnection =
            await _connectionManagement.SaveAsync(name, connectionString, cancellationToken);
        RemoveOrReplace(newConnection);
    }

    public async Task RemoveConnectionAsync(
        ServiceBusConnection serviceBusConnection,
        CancellationToken cancellationToken)
    {
        await _connectionManagement.DeleteAsync(serviceBusConnection.Name, cancellationToken);

        _serviceBusConnections.Remove(serviceBusConnection);
    }
    public ServiceBusConnection GetConnection(string name)
    {
        return ServiceBusConnections.Single(p => p.Name.EqualsInvariantIgnoreCase(name));
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
                menuItem.Topics.Add(topic);
                this.Notify(PropertyChanged);
            }

            menuItem.LoadingTopics = false;
            menuItem.TopicsLoaded = true;
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
        }


    }

    private void RemoveOrReplace(ServiceBusConnection newConnection)
    {
        var current = ServiceBusConnections.FirstOrDefault(p =>
            p.Name.EqualsInvariantIgnoreCase(newConnection.Name));

        if (current != null)
        {
            _serviceBusConnections.Remove(current);
        }

        _serviceBusConnections.Add(newConnection);
    }
}