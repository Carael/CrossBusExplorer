using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using CrossBusExplorer.Host;
using CrossBusExplorer.Host.Endpoints;
using CrossBusExplorer.Host.Jobs;
using CrossBusExplorer.Management;
using CrossBusExplorer.ServiceBus;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(
    Environment.GetEnvironmentVariable("CROSSBUS_API_URLS")
    ?? "http://127.0.0.1:0");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
});

builder.Services
    .AddSingleton<IManagementStorage, DesktopManagementStorage>()
    .AddManagement()
    .AddServiceBusServices()
    .AddSingleton<BackgroundJobManager>()
    .AddProblemDetails();
builder.Services.AddCors(options => options.AddPolicy(
    "DesktopDevelopment",
    policy => policy
        .WithOrigins("http://127.0.0.1:1420", "http://localhost:1420")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseCors("DesktopDevelopment");
}
app.Use(async (context, next) =>
{
    var expectedToken = Environment.GetEnvironmentVariable("CROSSBUS_API_TOKEN");
    if (string.IsNullOrEmpty(expectedToken) || context.Request.Path == "/health")
    {
        await next(context);
        return;
    }

    var authorization = context.Request.Headers.Authorization.ToString();
    const string bearerPrefix = "Bearer ";
    var suppliedToken = authorization.StartsWith(
        bearerPrefix,
        StringComparison.OrdinalIgnoreCase)
        ? authorization[bearerPrefix.Length..]
        : string.Empty;

    var valid = CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(expectedToken),
        Encoding.UTF8.GetBytes(suppliedToken));

    if (!valid)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    await next(context);
});

app.MapGet("/health", () => Results.Ok(new
{
    status = "ready",
    version = typeof(Program).Assembly.GetName().Version?.ToString()
}));

var api = app.MapGroup("/api/v1");
api.MapConnectionEndpoints();
api.MapEntityEndpoints();
api.MapMessageEndpoints();
api.MapJobEndpoints();

await app.StartAsync();

var addresses = app.Urls.OrderBy(value => value).ToArray();
var readyJson = JsonSerializer.Serialize(
    new { addresses },
    new JsonSerializerOptions
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    });
Console.WriteLine($"CROSSBUS_READY {readyJson}");
Console.Out.Flush();

await app.WaitForShutdownAsync();
