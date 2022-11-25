using CrossBusExplorer.ServiceBus.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace CrossBusExplorer.ServiceBus;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceBusServices(this IServiceCollection collection)
    {
        return collection
            .AddScoped<IQueueService, QueueService>()
            .AddScoped<ITopicService, TopicService>()
            .AddScoped<IMessageService, MessageService>()
            .AddScoped<IRuleService, RuleService>()
            .AddScoped<ISubscriptionService, SubscriptionService>();
    }
}