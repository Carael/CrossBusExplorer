using CrossBusExplorer.Host;
using CrossBusExplorer.Host.Mutations;
using CrossBusExplorer.Host.Queries;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSingleton<IConnectionManagement, ConnectionManagement>()
    .AddServiceBusServices()
    .AddGraphQLServer()
    .AddQueryType()
    .AddMutationType()
    .AddTypeExtension<QueueQueryExtensions>()
    .AddTypeExtension<MessageQueryExtensions>()
    .AddTypeExtension<QueueMutationExtensions>()
    .AddTypeExtension<MessagingMutationExtensions>()
    .AddTypeExtension<TopicQueryExtensions>()
    .AddTypeExtension<SubscriptionQueryExtensions>()
    .AddMutationConventions();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.UseWebSockets();
app.MapGraphQL();

app.Run();