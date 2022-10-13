﻿using CrossBusExplorer.Management;
using CrossBusExplorer.ServiceBus;
using CrossBusExplorer.Website;
using MudBlazor.Services;

namespace Maui;

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
		builder.Services.AddMudServices();
		builder.Services.AddWebsiteServices();
		builder.Services.AddServiceBusServices();
		builder.Services.AddManagement();
		builder.Services.AddSingleton<IManagementStorage, ManagementStorage>();

		return builder.Build();
	}
}