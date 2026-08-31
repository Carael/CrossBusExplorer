using CrossBusExplorer.ServiceBus.Contracts.Types;

namespace CrossBusExplorer.Host.Contracts;

public sealed record ReceiveMessagesRequest(
    string EntityName,
    string? SubscriptionName,
    SubQueue SubQueue,
    ReceiveMode Mode,
    ReceiveType Type,
    int? MessagesCount,
    long? FromSequenceNumber);

public sealed record SendMessagesRequest(
    string EntityName,
    IReadOnlyList<SendMessage> Messages);

public sealed record PurgeJobRequest(
    string ConnectionName,
    string EntityName,
    string? SubscriptionName,
    SubQueue SubQueue,
    long TotalCount);

public sealed record ResendJobRequest(
    string ConnectionName,
    string EntityName,
    string? SubscriptionName,
    SubQueue SubQueue,
    string DestinationEntityName,
    long TotalCount);

public sealed record DeleteMessageJobRequest(
    string ConnectionName,
    string EntityName,
    string? SubscriptionName,
    SubQueue SubQueue,
    long SequenceNumber);
