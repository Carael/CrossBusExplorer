using System.Text.Json;
using Blazored.LocalStorage;
using CrossBusExplorer.Website;
using CrossBusExplorer.Website.Models;
using ElectronNET.API;
using ElectronNET.API.Entities;
namespace Website.Host;

public class DefaultSettingsService : IUserSettingsService
{
    private const string FileName = "user_settings.json";

    public async Task<UserSettings> GetAsync(CancellationToken cancellationToken)
    {
        var filePath = await FilePath(cancellationToken);
        
        if (File.Exists(filePath))
        {
            var fileContent = await File.ReadAllTextAsync(filePath, cancellationToken);

            return JsonSerializer.Deserialize<UserSettings>(fileContent);
        }

        return new UserSettings();
    }
    
    public async Task SaveAsync(UserSettings userSettings, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(
            await FilePath(cancellationToken),
            JsonSerializer.Serialize(userSettings),
            cancellationToken);
    }
    
    private async Task<string> FilePath(CancellationToken cancellationToken)
    {
        var path = await Electron.App.GetPathAsync(PathName.UserData, cancellationToken);

        return Path.Combine(
            path,
            FileName);
    }  
}