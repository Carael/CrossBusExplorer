using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.Website.Models;
namespace CrossBusExplorer.Website;

public interface INavigationViewModel : INotifyPropertyChanged
{
    ObservableCollection<ConnectionFolder> Folders { get; }
    public Task LoadTopics(ConnectionMenuItem menuItem,  CancellationToken cancellationToken);
    public Task LoadQueues(ConnectionMenuItem menuItem, CancellationToken cancellationToken);
    Task LoadSubscriptionsAsync(string connectionName, TopicSubscriptionsModel model);
    public Task ReloadTopics(ConnectionMenuItem menuItem, CancellationToken cancellationToken);
    public Task ReloadQueues(ConnectionMenuItem menuItem, CancellationToken cancellationToken);
    public Task ReloadSubscriptions(string connectionName, TopicSubscriptionsModel model);
}