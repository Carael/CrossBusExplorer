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
    string ConnectionId,
    string EntityName,
    string? SubscriptionName,
    SubQueue SubQueue,
    long TotalCount);

public sealed record ResendJobRequest(
    string ConnectionId,
    string EntityName,
    string? SubscriptionName,
    SubQueue SubQueue,
    string DestinationEntityName,
    long TotalCount,
    IDictionary<string, object?>? ApplicationProperties = null);

public sealed record ExportMessagesRequest(
    string EntityName,
    string? SubscriptionName,
    string FolderPath,
    UploadFileType FileType,
    SubQueue SubQueue = SubQueue.None,
    ReceiveMode Mode = ReceiveMode.PeekLock,
    ReceiveType Type = ReceiveType.ByCount,
    int? MessagesCount = 50,
    long? FromSequenceNumber = null,
    IReadOnlyList<Message>? Messages = null);

public sealed record ExportMessagesResponse(
    long Count,
    string FolderPath);

public sealed record DefaultExportFolderResponse(
    string FolderPath);

public sealed record DeleteMessageJobRequest(
    string ConnectionId,
    string EntityName,
    string? SubscriptionName,
    SubQueue SubQueue,
    long SequenceNumber);
