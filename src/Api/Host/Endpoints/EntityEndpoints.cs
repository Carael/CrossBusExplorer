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
        var group = api.MapGroup("/connections/{connectionName}/queues")
            .WithTags("Queues");

        group.MapGet("/", (
            string connectionName,
            IQueueService service,
            CancellationToken cancellationToken) =>
            service.GetAsync(connectionName, cancellationToken));

        group.MapGet("/{queueName}", (
            string connectionName,
            string queueName,
            IQueueService service,
            CancellationToken cancellationToken) =>
            service.GetAsync(connectionName, queueName, cancellationToken));

        group.MapPost("/", (
            string connectionName,
            CreateQueueOptions options,
            IQueueService service,
            CancellationToken cancellationToken) =>
            service.CreateAsync(connectionName, options, cancellationToken));

        group.MapPut("/{queueName}", async (
            string connectionName,
            string queueName,
            UpdateQueueOptions options,
            IQueueService service,
            CancellationToken cancellationToken) =>
        {
            if (!queueName.Equals(options.Name, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error = "The route and request queue names must match." });
            }

            return Results.Ok(await service.UpdateAsync(connectionName, options, cancellationToken));
        });

        group.MapPost("/{queueName}/clone", (
            string connectionName,
            string queueName,
            CloneEntityRequest request,
            IQueueService service,
            CancellationToken cancellationToken) =>
            service.CloneAsync(
                connectionName,
                request.Name,
                queueName,
                cancellationToken));

        group.MapDelete("/{queueName}", (
            string connectionName,
            string queueName,
            IQueueService service,
            CancellationToken cancellationToken) =>
            service.DeleteAsync(connectionName, queueName, cancellationToken));
    }

    private static void MapTopicEndpoints(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/connections/{connectionName}/topics")
            .WithTags("Topics");

        group.MapGet("/", (
            string connectionName,
            ITopicService service,
            CancellationToken cancellationToken) =>
            service.GetStructureAsync(connectionName, cancellationToken));

        group.MapGet("/{topicName}", (
            string connectionName,
            string topicName,
            ITopicService service,
            CancellationToken cancellationToken) =>
            service.GetAsync(connectionName, topicName, cancellationToken));

        group.MapPost("/", (
            string connectionName,
            CreateTopicOptions options,
            ITopicService service,
            CancellationToken cancellationToken) =>
            service.CreateAsync(connectionName, options, cancellationToken));

        group.MapPut("/{topicName}", async (
            string connectionName,
            string topicName,
            UpdateTopicOptions options,
            ITopicService service,
            CancellationToken cancellationToken) =>
        {
            if (!topicName.Equals(options.Name, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error = "The route and request topic names must match." });
            }

            return Results.Ok(await service.UpdateAsync(connectionName, options, cancellationToken));
        });

        group.MapPost("/{topicName}/clone", (
            string connectionName,
            string topicName,
            CloneEntityRequest request,
            ITopicService service,
            CancellationToken cancellationToken) =>
            service.CloneAsync(
                connectionName,
                request.Name,
                topicName,
                cancellationToken));

        group.MapDelete("/{topicName}", (
            string connectionName,
            string topicName,
            ITopicService service,
            CancellationToken cancellationToken) =>
            service.DeleteAsync(connectionName, topicName, cancellationToken));
    }

    private static void MapSubscriptionEndpoints(RouteGroupBuilder api)
    {
        var group = api.MapGroup(
                "/connections/{connectionName}/topics/{topicName}/subscriptions")
            .WithTags("Subscriptions");

        group.MapGet("/", (
            string connectionName,
            string topicName,
            ISubscriptionService service,
            CancellationToken cancellationToken) =>
            service.GetAsync(connectionName, topicName, cancellationToken));

        group.MapGet("/{subscriptionName}", (
            string connectionName,
            string topicName,
            string subscriptionName,
            ISubscriptionService service,
            CancellationToken cancellationToken) =>
            service.GetAsync(
                connectionName,
                topicName,
                subscriptionName,
                cancellationToken));

        group.MapPost("/", async (
            string connectionName,
            string topicName,
            CreateSubscriptionOptions options,
            ISubscriptionService service,
            CancellationToken cancellationToken) =>
        {
            if (!topicName.Equals(options.TopicName, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error = "The route and request topic names must match." });
            }

            return Results.Ok(await service.CreateAsync(connectionName, options, cancellationToken));
        });

        group.MapPut("/{subscriptionName}", async (
            string connectionName,
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

            return Results.Ok(await service.UpdateAsync(connectionName, options, cancellationToken));
        });

        group.MapPost("/{subscriptionName}/clone", (
            string connectionName,
            string topicName,
            string subscriptionName,
            CloneEntityRequest request,
            ISubscriptionService service,
            CancellationToken cancellationToken) =>
            service.CloneAsync(
                connectionName,
                request.Name,
                topicName,
                subscriptionName,
                cancellationToken));

        group.MapDelete("/{subscriptionName}", (
            string connectionName,
            string topicName,
            string subscriptionName,
            ISubscriptionService service,
            CancellationToken cancellationToken) =>
            service.DeleteAsync(
                connectionName,
                topicName,
                subscriptionName,
                cancellationToken));
    }

    private static void MapRuleEndpoints(RouteGroupBuilder api)
    {
        var group = api.MapGroup(
                "/connections/{connectionName}/topics/{topicName}/subscriptions/{subscriptionName}/rules")
            .WithTags("Rules");

        group.MapGet("/", (
            string connectionName,
            string topicName,
            string subscriptionName,
            IRuleService service,
            CancellationToken cancellationToken) =>
            service.GetAsync(
                connectionName,
                topicName,
                subscriptionName,
                cancellationToken));

        group.MapPost("/", (
            string connectionName,
            string topicName,
            string subscriptionName,
            RuleRequest request,
            IRuleService service,
            CancellationToken cancellationToken) =>
            service.CreateAsync(
                connectionName,
                topicName,
                subscriptionName,
                request.Name,
                request.Type,
                request.Value,
                cancellationToken));

        group.MapPut("/{ruleName}", async (
            string connectionName,
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
                connectionName,
                topicName,
                subscriptionName,
                ruleName,
                request.Type,
                request.Value,
                cancellationToken));
        });

        group.MapDelete("/{ruleName}", (
            string connectionName,
            string topicName,
            string subscriptionName,
            string ruleName,
            IRuleService service,
            CancellationToken cancellationToken) =>
            service.DeleteAsync(
                connectionName,
                topicName,
                subscriptionName,
                ruleName,
                cancellationToken));
    }
}

public sealed record RuleRequest(string Name, RuleType Type, string? Value);
