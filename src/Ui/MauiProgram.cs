using CrossBusExplorer.Management;
using CrossBusExplorer.ServiceBus;
using Material.Blazor;

namespace Ui;

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

		builder.Services.AddMauiBlazorWebView();
#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif
		
		builder.Services.AddServiceBusServices();
		builder.Services.AddMBServices();
		builder.Services.AddSingleton<IConnectionManagement, ConnectionManagement>();


		return builder.Build();
	}
}
