namespace CrossBusExplorer.Management;

public interface IConnectionManagement
{
    Task<IReadOnlyList<ServiceBusConnection>> GetAsync(CancellationToken cancellationToken);
    Task<ServiceBusConnection> GetAsync(string name, CancellationToken cancellationToken);
    Task SaveAsync(string name, string connectionString, CancellationToken cancellationToken);
    Task DeleteAsync(string name, CancellationToken cancellationToken);
}