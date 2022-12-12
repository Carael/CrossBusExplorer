using System.Text.Json;
using Blazored.LocalStorage;
using CrossBusExplorer.Website;
using CrossBusExplorer.Website.Models;
namespace Website.Host;

public class DefaultSettingsService : IUserSettingsService
{
    private const string key = "user_settings";
    private readonly ILocalStorageService _localStorageService;

    public DefaultSettingsService(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }
    
    public async Task<UserSettings> GetAsync(CancellationToken cancellationToken)
    {
        if (await _localStorageService.ContainKeyAsync(key, cancellationToken))
        {
            return await _localStorageService.GetItemAsync<UserSettings>(key, cancellationToken);
        }

        return new UserSettings();
    }

    public async Task SaveAsync(UserSettings userSettings, CancellationToken cancellationToken)
    {
        await _localStorageService.SetItemAsync(key, userSettings, cancellationToken);
    }
}