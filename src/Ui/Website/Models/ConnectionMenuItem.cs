using System.Collections.ObjectModel;
using System.ComponentModel;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
namespace CrossBusExplorer.Website.Models;

public class ConnectionMenuItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public ConnectionMenuItem(string connectionName)
    {
        ConnectionName = connectionName;
        Queues = new ObservableCollection<QueueInfo>();
        Topics = new ObservableCollection<TopicSubscriptionsModel>();
    }
    
    public string ConnectionName { get; }
    
    private ObservableCollection<QueueInfo> _queues;
    public ObservableCollection<QueueInfo> Queues
    {
        get => _queues;
        private set
        {
            _queues = value;
            _queues.CollectionChanged += (_, _) =>
            {
                this.Notify(PropertyChanged);
            };
            this.Notify(PropertyChanged);
        }
    }
    
    private ObservableCollection<TopicSubscriptionsModel> _topics;
    public ObservableCollection<TopicSubscriptionsModel> Topics
    {
        get => _topics;
        private set
        {
            _topics = value;
            _topics.CollectionChanged += (_, _) =>
            {
                this.Notify(PropertyChanged);
            };
            this.Notify(PropertyChanged);
        }
    }
    public bool QueuesLoaded { get; set; }
    public bool TopicsLoaded { get; set; }
    public bool LoadingQueues { get; set; }
    public bool LoadingTopics { get; set; }
    public bool QueuesExpanded { get; set; }
    public bool TopicsExpanded { get; set; }
}