using System.Diagnostics;
using CrossBusExplorer.Host.Contracts;
using CrossBusExplorer.Management;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus.Contracts;

namespace CrossBusExplorer.Host.Endpoints;

public static class ConnectionEndpoints
{
    public static RouteGroupBuilder MapConnectionEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/connections").WithTags("Connections");

        group.MapGet("/", async (
            IConnectionManagement connections,
            CancellationToken cancellationToken) =>
        {
            var result = await connections.GetAsync(cancellationToken);
            return result.Select(ConnectionResponse.From);
        });

        group.MapGet("/{name}", async (
            string name,
            IConnectionManagement connections,
            CancellationToken cancellationToken) =>
            ConnectionResponse.From(await connections.GetAsync(name, cancellationToken)));

        group.MapPut("/{name}", async (
            string name,
            SaveConnectionRequest request,
            IConnectionManagement connections,
            CancellationToken cancellationToken) =>
        {
            if (!name.Equals(request.Name, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new
                {
                    error = "The route and request connection names must match."
                });
            }

            ServiceBusConnection connection;
            if (request.AuthenticationType == ServiceBusAuthenticationType.ConnectionString)
            {
                var connectionString = request.ConnectionString;
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    try
                    {
                        var existing = await connections.GetAsync(name, cancellationToken);
                        connectionString = existing.AuthenticationType ==
                                           ServiceBusAuthenticationType.ConnectionString
                            ? existing.ConnectionString
                            : null;
                    }
                    catch (ServiceBusConnectionDoesntExist)
                    {
                        // The validation response below is clearer for a new profile.
                    }
                }

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return Results.BadRequest(new
                    {
                        error = "Connection string is required for a new connection-string profile."
                    });
                }

                connection = ServiceBusConnectionStringHelper.GetServiceBusConnection(
                    request.Name,
                    connectionString,
                    request.TransportType,
                    request.Folder);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.FullyQualifiedName))
                {
                    return Results.BadRequest(new
                    {
                        error = "Fully qualified namespace is required."
                    });
                }

                connection = ServiceBusConnection.CreatePasswordless(
                    request.Name,
                    request.FullyQualifiedName,
                    request.AuthenticationType,
                    request.TransportType,
                    request.TenantId,
                    request.ClientId,
                    request.TokenFilePath,
                    request.Folder);
            }

            var saved = await connections.SaveAsync(connection, cancellationToken);
            return Results.Ok(ConnectionResponse.From(saved));
        });

        group.MapDelete("/{name}", async (
            string name,
            IConnectionManagement connections,
            CancellationToken cancellationToken) =>
        {
            await connections.DeleteAsync(name, cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/{name}/test", async (
            string name,
            IQueueService queues,
            CancellationToken cancellationToken) =>
        {
            var stopwatch = Stopwatch.StartNew();
            await foreach (var _ in queues.GetAsync(name, cancellationToken))
            {
                break;
            }

            stopwatch.Stop();
            return new ConnectionTestResponse(
                true,
                "Authentication and namespace access succeeded.",
                stopwatch.Elapsed);
        });

        return api;
    }
}
