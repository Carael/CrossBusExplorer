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
builder.Services.AddConfiguredMudServices();
builder.Services.AddServiceBusServices();
builder.Services.AddManagement();
builder.Services.AddSingleton<IManagementStorage, ManagementStorage>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();