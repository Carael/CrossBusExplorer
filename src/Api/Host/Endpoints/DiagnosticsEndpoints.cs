using CrossBusExplorer.Host.Diagnostics;

namespace CrossBusExplorer.Host.Endpoints;

public static class DiagnosticsEndpoints
{
    public static RouteGroupBuilder MapDiagnosticsEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/diagnostics").WithTags("Diagnostics");

        group.MapGet("/logs", (DiagnosticsLog log) => Results.Ok(log.Snapshot()));
        group.MapDelete("/logs", (DiagnosticsLog log) =>
        {
            log.Clear();
            return Results.NoContent();
        });

        return api;
    }
}
