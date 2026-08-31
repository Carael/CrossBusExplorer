using CrossBusExplorer.Host.Contracts;
using CrossBusExplorer.Host.Jobs;
using CrossBusExplorer.ServiceBus.Contracts;

namespace CrossBusExplorer.Host.Endpoints;

public static class JobEndpoints
{
    public static RouteGroupBuilder MapJobEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/jobs").WithTags("Jobs");

        group.MapGet("/", (BackgroundJobManager jobs) => jobs.GetAll());

        group.MapGet("/{id:guid}", (Guid id, BackgroundJobManager jobs) =>
        {
            var job = jobs.Get(id);
            return job is null ? Results.NotFound() : Results.Ok(job);
        });

        group.MapDelete("/{id:guid}", (Guid id, BackgroundJobManager jobs) =>
            jobs.Cancel(id) ? Results.Accepted() : Results.NotFound());

        group.MapPost("/purge", (PurgeJobRequest request, BackgroundJobManager jobs) =>
        {
            var job = jobs.Start(
                $"Purge {request.EntityName}",
                async (services, cancellationToken, progress) =>
                {
                    var messageService = services.GetRequiredService<IMessageService>();
                    long removed = 0;
                    await foreach (var result in messageService.PurgeAsync(
                                       request.ConnectionName,
                                       request.EntityName,
                                       request.SubscriptionName,
                                       request.SubQueue,
                                       request.TotalCount,
                                       cancellationToken))
                    {
                        removed = result.PurgedCount;
                        progress.Report(Percentage(removed, request.TotalCount));
                    }

                    return $"Purged {removed} messages.";
                });

            return Results.Accepted($"/api/v1/jobs/{job.Id}", job);
        });

        group.MapPost("/resend", (ResendJobRequest request, BackgroundJobManager jobs) =>
        {
            var job = jobs.Start(
                $"Resend {request.EntityName} to {request.DestinationEntityName}",
                async (services, cancellationToken, progress) =>
                {
                    var messageService = services.GetRequiredService<IMessageService>();
                    long resent = 0;
                    await foreach (var result in messageService.ResendAsync(
                                       request.ConnectionName,
                                       request.EntityName,
                                       request.SubscriptionName,
                                       request.SubQueue,
                                       request.DestinationEntityName,
                                       request.TotalCount,
                                       cancellationToken))
                    {
                        resent = result.ResendCount;
                        progress.Report(Percentage(resent, request.TotalCount));
                    }

                    return $"Resent {resent} messages.";
                });

            return Results.Accepted($"/api/v1/jobs/{job.Id}", job);
        });

        group.MapPost("/delete-message", (
            DeleteMessageJobRequest request,
            BackgroundJobManager jobs) =>
        {
            var job = jobs.Start(
                $"Delete message {request.SequenceNumber} from {request.EntityName}",
                async (services, cancellationToken, progress) =>
                {
                    var messageService = services.GetRequiredService<IMessageService>();
                    var result = await messageService.DeleteMessage(
                        request.ConnectionName,
                        request.EntityName,
                        request.SubscriptionName,
                        request.SubQueue,
                        request.SequenceNumber,
                        cancellationToken);

                    progress.Report(100);
                    return result.Count == 1
                        ? "Message deleted."
                        : "The message was no longer available and was not deleted.";
                });

            return Results.Accepted($"/api/v1/jobs/{job.Id}", job);
        });

        return api;
    }

    private static int Percentage(long current, long total) =>
        total <= 0 ? 100 : (int)Math.Clamp(current * 100 / total, 0, 100);
}
