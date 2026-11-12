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

        group.MapGet("/{id}", async (
            string id,
            IConnectionManagement connections,
            CancellationToken cancellationToken) =>
            ConnectionResponse.From(await connections.GetAsync(id, cancellationToken)));

        group.MapPut("/{id}", async (
            string id,
            SaveConnectionRequest request,
            IConnectionManagement connections,
            CancellationToken cancellationToken) =>
        {
            if (!string.IsNullOrWhiteSpace(request.Id) &&
                !id.Equals(request.Id, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new
                {
                    error = "The route and request connection IDs must match."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new
                {
                    error = "Connection display name is required."
                });
            }

            var displayName = request.Name.Trim();
            var connectionId = id.Trim();

            ServiceBusConnection connection;
            if (request.AuthenticationType == ServiceBusAuthenticationType.ConnectionString)
            {
                var connectionString = request.ConnectionString;
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    try
                    {
                        var existing = await connections.GetAsync(connectionId, cancellationToken);
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
                    displayName,
                    connectionString,
                    request.TransportType,
                    request.Folder,
                    connectionId);
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
                    displayName,
                    request.FullyQualifiedName,
                    request.AuthenticationType,
                    request.TransportType,
                    request.TenantId,
                    request.ClientId,
                    request.TokenFilePath,
                    request.Folder,
                    connectionId);
            }

            var saved = await connections.SaveAsync(connection, cancellationToken);
            return Results.Ok(ConnectionResponse.From(saved));
        });

        group.MapDelete("/{id}", async (
            string id,
            IConnectionManagement connections,
            CancellationToken cancellationToken) =>
        {
            await connections.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/{id}/test", async (
            string id,
            IQueueService queues,
            CancellationToken cancellationToken) =>
        {
            var stopwatch = Stopwatch.StartNew();
            await foreach (var _ in queues.GetAsync(id, cancellationToken))
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
