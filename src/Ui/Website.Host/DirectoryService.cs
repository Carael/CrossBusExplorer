using CrossBusExplorer.Website;
using ElectronSharp.API;
using ElectronSharp.API.Entities;
namespace Website.Host;

public class DirectoryService : IDirectoryService
{
    public async Task<string> GetDefaultDownloadDirectory(CancellationToken cancellationToken) =>
        HybridSupport.IsElectronActive
            ? await Electron.App.GetPathAsync(PathName.Downloads, cancellationToken)
            : Directory.GetCurrentDirectory();
}
