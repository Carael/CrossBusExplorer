using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.Management;
namespace CrossBusExplorer.Website;

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

    public async Task AddAsync(ServiceBusConnection connection, CancellationToken cancellationToken)
    {
        IDictionary<string, ServiceBusConnection> connections =
            await GetFileAsync(cancellationToken);

        if (connections.ContainsKey(connection.Name))
        {
            connections[connection.Name] = connection;
        }
        else
        {
            connections.Add(connection.Name, connection);
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

    private string FilePath => System.IO.Path.Combine(
        FileSystem.Current.AppDataDirectory,
        ServiceBusConnectionsFileName);
}