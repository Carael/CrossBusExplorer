using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.Management.Contracts;
namespace CrossBusExplorer.Website;

public interface IConnectionsViewModel: INotifyPropertyChanged
{
    ObservableCollection<ServiceBusConnection> ServiceBusConnections { get; }
    Task InitializeAsync(CancellationToken cancellationToken);
    Task SaveConnectionAsync(
        string name, 
        string connectionString,
        CancellationToken cancellationToken);
    Task RemoveConnectionAsync(ServiceBusConnection connection, CancellationToken cancellationToken);
}