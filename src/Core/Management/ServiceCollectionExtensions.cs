using CrossBusExplorer.Management.Contracts;
using Microsoft.Extensions.DependencyInjection;
namespace CrossBusExplorer.Management;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddManagement(this IServiceCollection collection)
    {
        return collection
            .AddScoped<IConnectionManagement, ConnectionManagement>();
    }
}