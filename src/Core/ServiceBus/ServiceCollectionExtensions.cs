using CrossBusExplorer.ServiceBus.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace CrossBusExplorer.ServiceBus;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceBusServices(this IServiceCollection collection)
    {
        return collection
            .AddSingleton<IQueueService, QueueService>()
            .AddSingleton<IMessageService, MessageService>();
    }
}