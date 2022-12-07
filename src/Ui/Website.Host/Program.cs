using Blazored.LocalStorage;
using CrossBusExplorer.Management;
using CrossBusExplorer.ServiceBus;
using CrossBusExplorer.Website;
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

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();