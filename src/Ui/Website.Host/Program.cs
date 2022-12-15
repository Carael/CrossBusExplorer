using CrossBusExplorer.Management;
using CrossBusExplorer.ServiceBus;
using CrossBusExplorer.Website;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Website.Host;

var builder = WebApplication.CreateBuilder(args);

StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddWebsiteServices();
builder.Services.AddServiceBusServices();
builder.Services.AddManagement();

builder.Services.AddScoped<IManagementStorage, ManagementStorage>();
builder.Services.AddScoped<IUserSettingsService, DefaultSettingsService>();

builder.Services.AddElectron();
builder.WebHost.UseElectron(args);

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

Task.Run(async () =>
{
    Electron.ReadAuth();

    var browserWindow = await Electron.WindowManager.CreateWindowAsync(
        new BrowserWindowOptions
        {
            UseContentSize = true,
            Title = "Cross Bus Explorer",
            WebPreferences = new WebPreferences
            {
                ZoomFactor = 1,
                ScrollBounce = true
            },
            Icon = "../../../icon512x512.png",
            Center = true,
            FullscreenWindowTitle = false
        });
    
    browserWindow.OnClose += () => app.StopAsync();
    browserWindow.OnReadyToShow += () => browserWindow.Show();
});

app.Run();