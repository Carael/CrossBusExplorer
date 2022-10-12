using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.Website.Models;
namespace CrossBusExplorer.Website;

public interface IMainViewModel : INotifyPropertyChanged
{
    ObservableCollection<ServiceBusConnection> ServiceBusConnections { get; }
    ObservableCollection<ConnectionMenuItem> MenuItems { get; } 
    Task InitializeAsync(CancellationToken cancellationToken);
    Task SaveConnectionAsync(
        string name,
        string connectionString,
        CancellationToken cancellationToken);
    Task RemoveConnectionAsync(
        ServiceBusConnection connection,
        CancellationToken cancellationToken);
    public ServiceBusConnection GetConnection(string name);

    public Task LoadTopics(ConnectionMenuItem menuItem,  CancellationToken cancellationToken);
    public Task LoadQueues(ConnectionMenuItem menuItem, CancellationToken cancellationToken);
}