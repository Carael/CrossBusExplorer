using CrossBusExplorer.Host.Contracts;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;

namespace CrossBusExplorer.Host.Endpoints;

public static class MessageEndpoints
{
    private const int MaxMessagesCount = 5000;

    public static RouteGroupBuilder MapMessageEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/messages/export-folder", () =>
            Results.Ok(new DefaultExportFolderResponse(GetDefaultExportFolder())))
            .WithTags("Messages");

        var group = api.MapGroup("/connections/{connectionId}/messages")
            .WithTags("Messages");

        group.MapPost("/receive", async (
            string connectionId,
            ReceiveMessagesRequest request,
            IMessageService service,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.EntityName))
            {
                return Results.BadRequest(new { error = "Entity name is required." });
            }
            if ((request.Type is ReceiveType.ByCount or ReceiveType.Newest) &&
                (request.MessagesCount is null or < 1 or > MaxMessagesCount))
            {
                return Results.BadRequest(new
                {
                    error = $"Messages count must be between 1 and {MaxMessagesCount}."
                });
            }
            if (request.Type == ReceiveType.Newest && request.Mode != ReceiveMode.PeekLock)
            {
                return Results.BadRequest(new { error = "Newest messages can only be peeked." });
            }
            if (request.FromSequenceNumber < 0)
            {
                return Results.BadRequest(new { error = "Sequence number cannot be negative." });
            }

            return Results.Ok(await service.GetMessagesAsync(
                connectionId,
                request.EntityName,
                request.SubscriptionName,
                request.SubQueue,
                request.Mode,
                request.Type,
                request.MessagesCount,
                request.FromSequenceNumber,
                cancellationToken));
        });

        group.MapPost("/send", async (
            string connectionId,
            SendMessagesRequest request,
            IMessageService service,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.EntityName) ||
                request.Messages is null ||
                request.Messages.Count == 0)
            {
                return Results.BadRequest(new
                {
                    error = "Entity name and at least one message are required."
                });
            }

            return Results.Ok(await service.SendMessagesAsync(
                connectionId,
                request.EntityName,
                request.Messages,
                cancellationToken));
        });

        group.MapPost("/export", async (
            string connectionId,
            ExportMessagesRequest request,
            IMessageService service,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.EntityName))
            {
                return Results.BadRequest(new { error = "Entity name is required." });
            }

            var folderPath = string.IsNullOrWhiteSpace(request.FolderPath)
                ? GetDefaultExportFolder()
                : request.FolderPath.Trim();

            if (request.Messages is not null)
            {
                var entityLabel = string.IsNullOrWhiteSpace(request.SubscriptionName)
                    ? request.EntityName
                    : $"{request.EntityName}_{request.SubscriptionName}";

                var exportedLoadedCount = await MessageExportFormatter.ExportMessagesToFolderAsync(
                    folderPath,
                    entityLabel,
                    request.Messages,
                    request.FileType,
                    cancellationToken);

                return Results.Ok(new ExportMessagesResponse(exportedLoadedCount, folderPath));
            }

            if ((request.Type is ReceiveType.ByCount or ReceiveType.Newest) &&
                (request.MessagesCount is null or < 1 or > MaxMessagesCount))
            {
                return Results.BadRequest(new
                {
                    error = $"Messages count must be between 1 and {MaxMessagesCount}."
                });
            }
            if (request.Type == ReceiveType.Newest && request.Mode != ReceiveMode.PeekLock)
            {
                return Results.BadRequest(new { error = "Newest messages can only be peeked." });
            }
            if (request.FromSequenceNumber < 0)
            {
                return Results.BadRequest(new { error = "Sequence number cannot be negative." });
            }

            var result = await service.ExportMessagesAsync(
                connectionId,
                request.EntityName,
                request.SubscriptionName,
                request.SubQueue,
                request.Mode,
                request.Type,
                request.MessagesCount,
                request.FromSequenceNumber,
                folderPath,
                request.FileType,
                cancellationToken);

            return Results.Ok(new ExportMessagesResponse(result.Count, folderPath));
        });

        return api;
    }

    private static string GetDefaultExportFolder()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            var downloads = Path.Combine(userProfile, "Downloads");
            if (Directory.Exists(downloads))
            {
                return downloads;
            }
            return userProfile;
        }

        return Path.GetTempPath();
    }
}
