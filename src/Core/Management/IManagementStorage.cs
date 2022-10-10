namespace CrossBusExplorer.Management;

public interface IManagementStorage
{
    Task StoreAsync(string content, CancellationToken cancellationToken);
    Task<string?> ReadAsync(CancellationToken cancellationToken);
}