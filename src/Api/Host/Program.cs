using CrossBusExplorer.Host.Queries;
using CrossBusExplorer.ServiceBus;
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddServiceBusServices()
    .AddGraphQLServer()
    .AddQueryType()
    .AddTypeExtension<ServiceBusQueryExtensions>()
    .AddMutationConventions();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGraphQL();

app.Run();