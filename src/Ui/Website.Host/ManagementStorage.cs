using System.Text;
using Blazored.LocalStorage;
using CrossBusExplorer.Management;
namespace Website.Host;

public class ManagementStorage : IManagementStorage
{
    private readonly ILocalStorageService _localStorageService;
    private const string key = "service_bus_connection";

    public ManagementStorage(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }
    
    public async Task StoreAsync(string content, CancellationToken cancellationToken)
    {
        await _localStorageService.SetItemAsStringAsync(key, content, cancellationToken);
    }
    public async Task<string?> ReadAsync(CancellationToken cancellationToken)
    {
        if (await _localStorageService.ContainKeyAsync(key, cancellationToken))
        {
            return await _localStorageService.GetItemAsStringAsync(key, cancellationToken);
        }

        return null;
    }
}