using CrossBusExplorer.Host.Tracking;
using CrossBusExplorer.Management;

namespace CrossBusExplorer.Host.Endpoints;

public static class TrackingEndpoints
{
    public static RouteGroupBuilder MapTrackingEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/tracking/sessions").WithTags("Tracking");

        group.MapGet("/", (TrackingSessionManager tracking) => tracking.GetSessions());

        group.MapPost("/", async (
            StartTrackingRequest request,
            TrackingSessionManager tracking,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.ConnectionName))
            {
                return Results.BadRequest(new { error = "Connection name is required." });
            }

            try
            {
                return Results.Ok(await tracking.StartSessionAsync(request, cancellationToken));
            }
            catch (ServiceBusConnectionDoesntExist exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        group.MapGet("/{id:guid}/updates", (
            Guid id,
            long? after,
            TrackingSessionManager tracking) =>
        {
            var updates = tracking.GetUpdates(id, after ?? 0);
            return updates is null ? Results.NotFound() : Results.Ok(updates);
        });

        group.MapPost("/{id:guid}/stop", async (Guid id, TrackingSessionManager tracking) =>
        {
            var session = await tracking.StopSessionAsync(id);
            return session is null ? Results.NotFound() : Results.Ok(session);
        });

        group.MapDelete("/{id:guid}", async (Guid id, TrackingSessionManager tracking) =>
            await tracking.RemoveSessionAsync(id) ? Results.NoContent() : Results.NotFound());

        return api;
    }
}
