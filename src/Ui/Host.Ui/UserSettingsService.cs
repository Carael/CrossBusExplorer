using System.Text.Json;
using CrossBusExplorer.Website;
using CrossBusExplorer.Website.Models;
namespace CrossBusExplorer.Host.Ui;

public class UserSettingsService : IUserSettingsService
{
    private const string key = "user_settings";

    public Task<UserSettings> GetAsync(CancellationToken cancellationToken)
    {
        if (!Preferences.ContainsKey(key))
        {
            return Task.FromResult(new UserSettings());
        }

        var json = Preferences.Get(key, null);

        if (json is null)
        {
            return Task.FromResult(new UserSettings());
        }

        return Task.FromResult(JsonSerializer.Deserialize<UserSettings>(json)
                               ?? new UserSettings());
    }
    
    public Task SaveAsync(UserSettings userSettings, CancellationToken cancellationToken)
    {
        Preferences.Set(key, JsonSerializer.Serialize(userSettings));

        return Task.CompletedTask;
    }
}