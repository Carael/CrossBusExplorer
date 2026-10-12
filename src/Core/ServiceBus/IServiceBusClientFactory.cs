using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.Management.Contracts;

namespace CrossBusExplorer.ServiceBus;

public interface IServiceBusClientFactory
{
    ServiceBusClient GetClient(ServiceBusConnection connection);

    ServiceBusAdministrationClient GetAdministrationClient(ServiceBusConnection connection);
}
