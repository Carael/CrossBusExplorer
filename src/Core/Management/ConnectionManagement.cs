using System.Text;
using System.Text.Json;
using CrossBusExplorer.Management.Contracts;
namespace CrossBusExplorer.Management;

public class ConnectionManagement : IConnectionManagement
{
    private readonly IManagementStorage _managementStorage;

    public ConnectionManagement(IManagementStorage managementStorage)
    {
        _managementStorage = managementStorage;

    }

    public async Task<IReadOnlyList<ServiceBusConnection>> GetAsync(
        CancellationToken cancellationToken)
    {
        return (await GetData(cancellationToken)).Select(p => p.Value).ToList();
    }

    public async Task<ServiceBusConnection> GetAsync(string name,
        CancellationToken cancellationToken)
    {
        var connections = await GetData(cancellationToken);

        if (connections.ContainsKey(name))
        {
            return connections[name];
        }

        throw new ServiceBusConnectionDoesntExist(name);
    }

    public async Task SaveAsync(
        string name,
        string connectionString,
        CancellationToken cancellationToken)
    {
        IDictionary<string, ServiceBusConnection> connections =
            await GetData(cancellationToken);

        var connection = ServiceBusConnectionStringHelper.GetServiceBusConnection(
            name, connectionString);

        if (connections.ContainsKey(name))
        {
            connections[name] = connection;
        }
        else if (connections.Any(p => p.Value.ConnectionString == connectionString))
        {
            var keyToRemove = connections.First(p => p.Value.ConnectionString == connectionString)
                .Key;

            connections.Remove(keyToRemove);
            connections.Add(name, connection);
        }
        else
        {
            connections.Add(name, connection);
        }

        await SaveAsync(connections, cancellationToken);
    }

    public async Task DeleteAsync(string name, CancellationToken cancellationToken)
    {
        var connections = await GetData(cancellationToken);

        if (connections.ContainsKey(name))
        {
            connections.Remove(name);

            await SaveAsync(connections, cancellationToken);
        }
        else
        {
            throw new ServiceBusConnectionDoesntExist(name);
        }
    }

    private async Task<IDictionary<string, ServiceBusConnection>> GetData(
        CancellationToken cancellationToken)
    {
        var data = await _managementStorage.ReadAsync(cancellationToken);

        if (data == null)
        {
            return new Dictionary<string, ServiceBusConnection>();
        }

        return JsonSerializer.Deserialize<IDictionary<string, ServiceBusConnection>>(
            data) ?? new Dictionary<string, ServiceBusConnection>();
    }

    private async Task SaveAsync(
        IDictionary<string, ServiceBusConnection> connections,
        CancellationToken cancellationToken)
    {
        await _managementStorage.StoreAsync(
            JsonSerializer.Serialize(connections),
            cancellationToken);
    }
}