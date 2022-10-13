using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.Website.Models;
namespace CrossBusExplorer.Website;

public interface IConnectionsViewModel : INotifyPropertyChanged
{
    ObservableCollection<ServiceBusConnection> ServiceBusConnections { get; }
    Task InitializeAsync(CancellationToken cancellationToken);
    Task SaveConnectionAsync(
        string name,
        string connectionString,
        CancellationToken cancellationToken);
    Task RemoveConnectionAsync(
        ServiceBusConnection connection,
        CancellationToken cancellationToken);
    public ServiceBusConnection GetConnection(string name);
}