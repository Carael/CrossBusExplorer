using CrossBusExplorer.ServiceBus;
using Material.Blazor;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Ui.Data;

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
		
		builder.Services.AddSingleton<WeatherForecastService>();
		builder.Services.AddServiceBusServices();
		builder.Services.AddMBServices();


		return builder.Build();
	}
}
