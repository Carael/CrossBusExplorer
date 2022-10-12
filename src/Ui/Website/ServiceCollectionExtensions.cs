using CrossBusExplorer.Website.ViewModels;
using Fluxor.Extensions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
namespace CrossBusExplorer.Website;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebsiteServices(this IServiceCollection collection)
    {
        collection.AddScoped<IConnectionsViewModel, ConnectionsViewModel>();
        
        return collection.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;

            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = false;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 10000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        });
    }
}