namespace CrossBusExplorer.Management.Contracts;

public interface IConnectionManagement
{
    Task<IList<ServiceBusConnection>> GetAsync(CancellationToken cancellationToken);
    Task<ServiceBusConnection> GetAsync(string name, CancellationToken cancellationToken);
    Task<ServiceBusConnection> SaveAsync(
        string name, 
        string connectionString,
        CancellationToken cancellationToken);
    Task DeleteAsync(string name, CancellationToken cancellationToken);
}