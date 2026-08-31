using CrossBusExplorer.Host.Contracts;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;

namespace CrossBusExplorer.Host.Endpoints;

public static class MessageEndpoints
{
    public static RouteGroupBuilder MapMessageEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/connections/{connectionName}/messages")
            .WithTags("Messages");

        group.MapPost("/receive", async (
            string connectionName,
            ReceiveMessagesRequest request,
            IMessageService service,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.EntityName))
            {
                return Results.BadRequest(new { error = "Entity name is required." });
            }
            if (request.Type == ReceiveType.ByCount &&
                (request.MessagesCount is null or < 1 or > 250))
            {
                return Results.BadRequest(new
                {
                    error = "Messages count must be between 1 and 250 when receiving by count."
                });
            }
            if (request.FromSequenceNumber < 0)
            {
                return Results.BadRequest(new { error = "Sequence number cannot be negative." });
            }

            return Results.Ok(await service.GetMessagesAsync(
                connectionName,
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
            string connectionName,
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
                connectionName,
                request.EntityName,
                request.Messages,
                cancellationToken));
        });

        return api;
    }
}
