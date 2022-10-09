namespace CrossBusExplorer.Management;

public interface IConnectionManagement
{
    Task<IList<ServiceBusConnection>> GetAsync(CancellationToken cancellationToken);
    Task<ServiceBusConnection> GetAsync(string name, CancellationToken cancellationToken);
    Task AddAsync(ServiceBusConnection connection, CancellationToken cancellationToken);
    Task DeleteAsync(string name, CancellationToken cancellationToken);
}