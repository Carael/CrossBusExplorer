using System.Threading;
using System.Threading.Tasks;
namespace CrossBusExplorer.Website;

public interface IDirectoryService
{
    Task<string> GetDefaultDownloadDirectory(CancellationToken cancellationToken);
}
