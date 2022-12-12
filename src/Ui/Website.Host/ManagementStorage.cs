using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using CrossBusExplorer.Management;
using CrossBusExplorer.Management.Contracts;
namespace Website.Host;

public class ManagementStorage : IManagementStorage
{
    private readonly ILocalStorageService _localStorageService;
    private const string key = "service_bus_connection";

    public ManagementStorage(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }

    public async Task StoreAsync(
        IDictionary<string, ServiceBusConnection> connections,
        CancellationToken cancellationToken)
    {
        await _localStorageService.SetItemAsStringAsync(
            key,
            JsonSerializer.Serialize(connections),
            cancellationToken);
    }
    public async Task<IDictionary<string, ServiceBusConnection>> ReadAsync(
        CancellationToken cancellationToken)
    {
        if (await _localStorageService.ContainKeyAsync(key, cancellationToken))
        {
            var serializedData =
                await _localStorageService.GetItemAsStringAsync(key, cancellationToken);
            
            return JsonSerializer.Deserialize<IDictionary<string, ServiceBusConnection>>(
                serializedData) ?? new Dictionary<string, ServiceBusConnection>();
        }

        return new Dictionary<string, ServiceBusConnection>();
    }

}