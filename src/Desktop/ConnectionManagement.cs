using System.Text;
using System.Text.Json;
using CrossBusExplorer.Management;
namespace Ui;

public class ConnectionManagement : IConnectionManagement
{
    private const string ServiceBusConnectionsFileName = "servicebusconnections.json";

    public async Task<IList<ServiceBusConnection>> GetAsync(
        CancellationToken cancellationToken)
    {
        return (await GetFileAsync(cancellationToken)).Select(p => p.Value).ToList();
    }

    public async Task<ServiceBusConnection> GetAsync(string name,
        CancellationToken cancellationToken)
    {
        var connections = await GetFileAsync(cancellationToken);

        if (connections.ContainsKey(name))
        {
            return connections[name];
        }

        throw new ServiceBusConnectionDoesntExist(name);
    }

    public async Task SaveAsync(string name, string connectionString, CancellationToken cancellationToken)
    {
        IDictionary<string, ServiceBusConnection> connections =
            await GetFileAsync(cancellationToken);

        var connection =
            ServiceBusConnectionStringHelper.GetServiceBusConnection(name, connectionString);
        
        if (connections.ContainsKey(name))
        {
            connections[name] = connection;
        }
        else
        {
            connections.Add(name, connection);
        }

        await SaveAsync(connections, cancellationToken);
    }

    public async Task DeleteAsync(string name, CancellationToken cancellationToken)
    {
        var connections = await GetFileAsync(cancellationToken);

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

    private async Task<IDictionary<string, ServiceBusConnection>> GetFileAsync(
        CancellationToken cancellationToken)
    {
        if (File.Exists(FilePath))
        {
            var result = JsonSerializer.Deserialize<IDictionary<string, ServiceBusConnection>>(
                File.ReadAllText(FilePath));

            return result ?? new Dictionary<string, ServiceBusConnection>();
        }

        return new Dictionary<string, ServiceBusConnection>();
    }

    private async Task SaveAsync(
        IDictionary<string, ServiceBusConnection> connections,
        CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(
            FilePath,
            JsonSerializer.Serialize(connections),
            Encoding.UTF8,
            cancellationToken);
    }

    private string FilePath => Path.Combine(
        FileSystem.Current.AppDataDirectory,
        ServiceBusConnectionsFileName);
}