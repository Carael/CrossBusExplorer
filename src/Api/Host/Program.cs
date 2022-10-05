using CrossBusExplorer.Host.Mutations;
using CrossBusExplorer.Host.Queries;
using CrossBusExplorer.ServiceBus;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddServiceBusServices()
    .AddGraphQLServer()
    .AddQueryType()
    // .AddMutationType()
    .AddTypeExtension<QueueQueryExtensions>()
    .AddTypeExtension<MessageQueryExtensions>()
    // .AddTypeExtension<QueueMutationExtensions>()
    .AddMutationConventions();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGraphQL();

app.Run();