using CrossBusExplorer.Management.Contracts;
namespace CrossBusExplorer.Host;

public class ConnectionManagement : IConnectionManagement
{
    public Task<IList<ServiceBusConnection>> GetAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    public Task<ServiceBusConnection> GetAsync(string name, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    public Task<ServiceBusConnection> SaveAsync(string name, string connectionString, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    public Task DeleteAsync(string name, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}