using System.Text.Json;
using Blazored.LocalStorage;
using CrossBusExplorer.Website;
using CrossBusExplorer.Website.Models;
namespace Website.Host;

public class DefaultSettingsService : IUserSettingsService
{
    private const string FileName = "user_settings.json";

    public async Task<UserSettings> GetAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(FilePath))
        {
            var fileContent = await File.ReadAllTextAsync(FilePath, cancellationToken);

            return JsonSerializer.Deserialize<UserSettings>(fileContent);
        }

        return new UserSettings();
    }
    
    public async Task SaveAsync(UserSettings userSettings, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(
            FilePath,
            JsonSerializer.Serialize(userSettings),
            cancellationToken);
    }
    
    private string FilePath => Path.Combine(
        Directory.GetCurrentDirectory(),
        FileName);
}