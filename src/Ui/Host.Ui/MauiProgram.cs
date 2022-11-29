using CrossBusExplorer.Management;
using CrossBusExplorer.ServiceBus;
using CrossBusExplorer.Website;
using MudBlazor.Services;
namespace CrossBusExplorer.Host.Ui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});
#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Services.AddBlazorWebView();
#endif

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddMudServices();
		builder.Services.AddWebsiteServices();
		builder.Services.AddServiceBusServices();
		builder.Services.AddManagement();
		builder.Services.AddSingleton<IManagementStorage, ManagementStorage>();
		builder.Services.AddSingleton<IUserSettingsService, UserSettingsService>();

		return builder.Build();
	}
}