using CrossBusExplorer.Host.Contracts;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;

namespace CrossBusExplorer.Host.Endpoints;

public static class EntityEndpoints
{
    public static RouteGroupBuilder MapEntityEndpoints(this RouteGroupBuilder api)
    {
        MapQueueEndpoints(api);
        MapTopicEndpoints(api);
        MapSubscriptionEndpoints(api);
        MapRuleEndpoints(api);
        return api;
    }

    private static void MapQueueEndpoints(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/connections/{connectionId}/queues")
            .WithTags("Queues");

        group.MapGet("/", (
            string connectionId,
            IQueueService service,
            CancellationToken cancellationToken) =>
            service.GetAsync(connectionId, cancellationToken));

        group.MapGet("/{queueName}", (
            string connectionId,
            string queueName,
            IQueueService service,
            CancellationToken cancellationToken) =>
            service.GetAsync(connectionId, queueName, cancellationToken));

        group.MapPost("/", (
            string connectionId,
            CreateQueueOptions options,
            IQueueService service,
            CancellationToken cancellationToken) =>
            service.CreateAsync(connectionId, options, cancellationToken));

        group.MapPut("/{queueName}", async (
            string connectionId,
            string queueName,
            UpdateQueueOptions options,
            IQueueService service,
            CancellationToken cancellationToken) =>
        {
            if (!queueName.Equals(options.Name, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error = "The route and request queue names must match." });
            }

            return Results.Ok(await service.UpdateAsync(connectionId, options, cancellationToken));
        });

        group.MapPost("/{queueName}/clone", (
            string connectionId,
            string queueName,
            CloneEntityRequest request,
            IQueueService service,
            CancellationToken cancellationToken) =>
            service.CloneAsync(
                connectionId,
                request.Name,
                queueName,
                cancellationToken));

        group.MapDelete("/{queueName}", (
            string connectionId,
            string queueName,
            IQueueService service,
            CancellationToken cancellationToken) =>
            service.DeleteAsync(connectionId, queueName, cancellationToken));
    }

    private static void MapTopicEndpoints(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/connections/{connectionId}/topics")
            .WithTags("Topics");

        group.MapGet("/", (
            string connectionId,
            ITopicService service,
            CancellationToken cancellationToken) =>
            service.GetStructureAsync(connectionId, cancellationToken));

        group.MapGet("/{topicName}", (
            string connectionId,
            string topicName,
            ITopicService service,
            CancellationToken cancellationToken) =>
            service.GetAsync(connectionId, topicName, cancellationToken));

        group.MapPost("/", (
            string connectionId,
            CreateTopicOptions options,
            ITopicService service,
            CancellationToken cancellationToken) =>
            service.CreateAsync(connectionId, options, cancellationToken));

        group.MapPut("/{topicName}", async (
            string connectionId,
            string topicName,
            UpdateTopicOptions options,
            ITopicService service,
            CancellationToken cancellationToken) =>
        {
            if (!topicName.Equals(options.Name, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error = "The route and request topic names must match." });
            }

            return Results.Ok(await service.UpdateAsync(connectionId, options, cancellationToken));
        });

        group.MapPost("/{topicName}/clone", (
            string connectionId,
            string topicName,
            CloneEntityRequest request,
            ITopicService service,
            CancellationToken cancellationToken) =>
            service.CloneAsync(
                connectionId,
                request.Name,
                topicName,
                cancellationToken));

        group.MapDelete("/{topicName}", (
            string connectionId,
            string topicName,
            ITopicService service,
            CancellationToken cancellationToken) =>
            service.DeleteAsync(connectionId, topicName, cancellationToken));
    }

    private static void MapSubscriptionEndpoints(RouteGroupBuilder api)
    {
        var group = api.MapGroup(
                "/connections/{connectionId}/topics/{topicName}/subscriptions")
            .WithTags("Subscriptions");

        group.MapGet("/", (
            string connectionId,
            string topicName,
            ISubscriptionService service,
            CancellationToken cancellationToken) =>
            service.GetAsync(connectionId, topicName, cancellationToken));

        group.MapGet("/{subscriptionName}", (
            string connectionId,
            string topicName,
            string subscriptionName,
            ISubscriptionService service,
            CancellationToken cancellationToken) =>
            service.GetAsync(
                connectionId,
                topicName,
                subscriptionName,
                cancellationToken));

        group.MapPost("/", async (
            string connectionId,
            string topicName,
            CreateSubscriptionOptions options,
            ISubscriptionService service,
            CancellationToken cancellationToken) =>
        {
            if (!topicName.Equals(options.TopicName, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error = "The route and request topic names must match." });
            }

            return Results.Ok(await service.CreateAsync(connectionId, options, cancellationToken));
        });

        group.MapPut("/{subscriptionName}", async (
            string connectionId,
            string topicName,
            string subscriptionName,
            UpdateSubscriptionOptions options,
            ISubscriptionService service,
            CancellationToken cancellationToken) =>
        {
            if (!topicName.Equals(options.TopicName, StringComparison.OrdinalIgnoreCase) ||
                !subscriptionName.Equals(
                    options.SubscriptionName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new
                {
                    error = "The route and request topic/subscription names must match."
                });
            }

            return Results.Ok(await service.UpdateAsync(connectionId, options, cancellationToken));
        });

        group.MapPost("/{subscriptionName}/clone", (
            string connectionId,
            string topicName,
            string subscriptionName,
            CloneEntityRequest request,
            ISubscriptionService service,
            CancellationToken cancellationToken) =>
            service.CloneAsync(
                connectionId,
                request.Name,
                topicName,
                subscriptionName,
                cancellationToken));

        group.MapDelete("/{subscriptionName}", (
            string connectionId,
            string topicName,
            string subscriptionName,
            ISubscriptionService service,
            CancellationToken cancellationToken) =>
            service.DeleteAsync(
                connectionId,
                topicName,
                subscriptionName,
                cancellationToken));
    }

    private static void MapRuleEndpoints(RouteGroupBuilder api)
    {
        var group = api.MapGroup(
                "/connections/{connectionId}/topics/{topicName}/subscriptions/{subscriptionName}/rules")
            .WithTags("Rules");

        group.MapGet("/", (
            string connectionId,
            string topicName,
            string subscriptionName,
            IRuleService service,
            CancellationToken cancellationToken) =>
            service.GetAsync(
                connectionId,
                topicName,
                subscriptionName,
                cancellationToken));

        group.MapPost("/", (
            string connectionId,
            string topicName,
            string subscriptionName,
            RuleRequest request,
            IRuleService service,
            CancellationToken cancellationToken) =>
            service.CreateAsync(
                connectionId,
                topicName,
                subscriptionName,
                request.Name,
                request.Type,
                request.Value,
                cancellationToken));

        group.MapPut("/{ruleName}", async (
            string connectionId,
            string topicName,
            string subscriptionName,
            string ruleName,
            RuleRequest request,
            IRuleService service,
            CancellationToken cancellationToken) =>
        {
            if (!ruleName.Equals(request.Name, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error = "The route and request rule names must match." });
            }

            return Results.Ok(await service.UpdateAsync(
                connectionId,
                topicName,
                subscriptionName,
                ruleName,
                request.Type,
                request.Value,
                cancellationToken));
        });

        group.MapDelete("/{ruleName}", (
            string connectionId,
            string topicName,
            string subscriptionName,
            string ruleName,
            IRuleService service,
            CancellationToken cancellationToken) =>
            service.DeleteAsync(
                connectionId,
                topicName,
                subscriptionName,
                ruleName,
                cancellationToken));
    }
}

public sealed record RuleRequest(string Name, RuleType Type, string? Value);
