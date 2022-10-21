using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.Website.Extensions;
namespace CrossBusExplorer.Website.ViewModels;

public class ConnectionsViewModel : IConnectionsViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly IConnectionManagement _connectionManagement;


    private ObservableCollection<ServiceBusConnection> _serviceBusConnections =
        new ObservableCollection<ServiceBusConnection>();

    public ConnectionsViewModel(
        IConnectionManagement connectionManagement)
    {
        _connectionManagement = connectionManagement;
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

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        ServiceBusConnections =
            new ObservableCollection<ServiceBusConnection>(
                await _connectionManagement.GetAsync(default));
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