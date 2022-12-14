using CrossBusExplorer.Management.Contracts;
namespace CrossBusExplorer.Management;

public interface IManagementStorage
{
    Task StoreAsync(
        IDictionary<string, ServiceBusConnection> connections,
        CancellationToken cancellationToken);
    Task<IDictionary<string, ServiceBusConnection>> ReadAsync(CancellationToken cancellationToken);
}