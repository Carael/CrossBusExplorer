using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.Website.Models;
namespace CrossBusExplorer.Website;

public interface ISettingsService
{
    Task<UserSettings> GetAsync(CancellationToken cancellationToken);
    Task SaveAsync(UserSettings userSettings, CancellationToken cancellationToken);
}