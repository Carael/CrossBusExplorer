using Blazored.LocalStorage;
using CrossBusExplorer.Management;
using CrossBusExplorer.ServiceBus;
using CrossBusExplorer.Website;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Website.Host;

var builder = WebApplication.CreateBuilder(args);

StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddWebsiteServices();
builder.Services.AddServiceBusServices();
builder.Services.AddManagement();
builder.Services.AddScoped<IManagementStorage, ManagementStorage>();
builder.Services.AddScoped<IUserSettingsService, DefaultSettingsService>();
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddElectron();
builder.WebHost.UseElectron(args);


var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

//TODO: register hosted service that will start this?
Task.Run(async () =>
{
    Electron.ReadAuth();
    await Task.Delay(500);

    
    
    var browserWindow = await Electron.WindowManager.CreateWindowAsync(
        new BrowserWindowOptions
        {
            ZoomToPageWidth = true,
            WebPreferences = new WebPreferences
            {
                ZoomFactor = 1
            }
        });
    
    await browserWindow.WebContents.Session.ClearCacheAsync();
    browserWindow.OnClose += () => app.StopAsync();
    browserWindow.OnReadyToShow += () => browserWindow.Show();
});

app.Run();