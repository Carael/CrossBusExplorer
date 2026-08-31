using CrossBusExplorer.Management.Contracts;
namespace CrossBusExplorer.Management;

public class ConnectionManagement : IConnectionManagement
{
    private readonly IManagementStorage _managementStorage;

    public ConnectionManagement(IManagementStorage managementStorage)
    {
        _managementStorage = managementStorage;
    }

    public async Task<IList<ServiceBusConnection>> GetAsync(
        CancellationToken cancellationToken)
    {
        return (await _managementStorage.ReadAsync(cancellationToken))
            .Select(p => p.Value)
            .ToList();
    }

    public async Task<ServiceBusConnection> GetAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var connections = await _managementStorage.ReadAsync(cancellationToken);

        if (connections.ContainsKey(name))
        {
            return connections[name];
        }

        throw new ServiceBusConnectionDoesntExist(name);
    }

    public async Task<ServiceBusConnection> SaveAsync(
        string name,
        string connectionString,
        string folder,
        ServiceBusTransportType transportType,
        CancellationToken cancellationToken)
    {
        var connection = ServiceBusConnectionStringHelper.GetServiceBusConnection(
            name, connectionString, transportType, folder);

        return await SaveAsync(connection, cancellationToken);
    }

    public async Task<ServiceBusConnection> SaveAsync(
        ServiceBusConnection connection,
        CancellationToken cancellationToken)
    {
        IDictionary<string, ServiceBusConnection> connections =
            await _managementStorage.ReadAsync(cancellationToken);

        if (connections.ContainsKey(connection.Name))
        {
            connections[connection.Name] = connection;
        }
        else if (connection.ConnectionString is not null &&
                 connections.Any(p => p.Value.ConnectionString == connection.ConnectionString))
        {
            var keyToRemove = connections
                .First(p => p.Value.ConnectionString == connection.ConnectionString)
                .Key;

            connections.Remove(keyToRemove);
            connections.Add(connection.Name, connection);
        }
        else
        {
            connections.Add(connection.Name, connection);
        }

        await _managementStorage.StoreAsync(
            connections,
            cancellationToken);

        return connection;
    }

    public async Task DeleteAsync(string name, CancellationToken cancellationToken)
    {
        var connections = await _managementStorage.ReadAsync(cancellationToken);

        if (connections.ContainsKey(name))
        {
            connections.Remove(name);

            await _managementStorage.StoreAsync(
                connections,
                cancellationToken);
        }
        else
        {
            throw new ServiceBusConnectionDoesntExist(name);
        }
    }
}
