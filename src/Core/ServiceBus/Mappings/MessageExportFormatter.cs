using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrossBusExplorer.ServiceBus.Contracts.Types;

namespace CrossBusExplorer.ServiceBus.Mappings;

public sealed record CrossBusExplorerExportedMessage(
    string Format,
    string? Id,
    string? Subject,
    string? ContentType,
    string? CorrelationId,
    string? PartitionKey,
    string? ReplyTo,
    string? ReplyToSessionId,
    string? SessionId,
    string? To,
    string Body,
    Dictionary<string, object?>? ApplicationProperties,
    MessageSystemProperties? SystemProperties = null);

public static class MessageExportFormatter
{
    public const string FormatIdentifier = "CrossBusExplorer/1.0";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static string FormatMessage(Message message, UploadFileType fileType)
    {
        if (fileType == UploadFileType.Body)
        {
            return message.Body;
        }

        var exported = new CrossBusExplorerExportedMessage(
            FormatIdentifier,
            message.Id,
            message.Subject,
            message.SystemProperties.ContentType,
            message.SystemProperties.CorrelationId,
            message.SystemProperties.PartitionKey,
            message.SystemProperties.ReplyTo,
            message.SystemProperties.ReplyToSessionId,
            message.SystemProperties.SessionId,
            message.SystemProperties.To,
            message.Body,
            message.ApplicationProperties?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            message.SystemProperties);

        return JsonSerializer.Serialize(exported, SerializerOptions);
    }

    public static SendMessage ParseMessage(string fileContent)
    {
        if (!string.IsNullOrWhiteSpace(fileContent))
        {
            try
            {
                using var document = JsonDocument.Parse(fileContent);
                if (document.RootElement.ValueKind == JsonValueKind.Object &&
                    IsCrossBusExplorerFormat(document.RootElement))
                {
                    var exported = document.RootElement.Deserialize<CrossBusExplorerExportedMessage>(SerializerOptions);
                    if (exported is not null)
                    {
                        return new SendMessage(
                            exported.Body ?? string.Empty,
                            exported.Subject,
                            exported.To,
                            exported.ContentType,
                            exported.CorrelationId,
                            exported.Id,
                            exported.PartitionKey,
                            exported.ReplyTo,
                            exported.SessionId,
                            null,
                            null,
                            exported.ApplicationProperties);
                    }
                }
            }
            catch (JsonException)
            {
                // Fallback to plain body content when not valid JSON.
            }
        }

        return SendMessage.CreateFromBody(fileContent);
    }

    public static async Task<int> ExportMessagesToFolderAsync(
        string folderPath,
        string entityName,
        IReadOnlyList<Message> messages,
        UploadFileType fileType,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        Directory.CreateDirectory(folderPath);

        var safeEntity = SanitizeFileName(string.IsNullOrWhiteSpace(entityName) ? "message" : entityName);
        var count = 0;

        for (var i = 0; i < messages.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = messages[i];
            var content = FormatMessage(message, fileType);
            var extension = GetFileExtension(message, fileType);
            var sequencePart = message.SystemProperties.SequenceNumber > 0
                ? message.SystemProperties.SequenceNumber.ToString()
                : (i + 1).ToString();
            var idPart = string.IsNullOrWhiteSpace(message.Id)
                ? Guid.NewGuid().ToString("N")[..8]
                : SanitizeFileName(message.Id);

            var baseName = $"{safeEntity}_{sequencePart}_{idPart}";
            var filePath = GetUniqueFilePath(folderPath, baseName, extension);

            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken);
            count++;
        }

        return count;
    }

    private static bool IsCrossBusExplorerFormat(JsonElement root)
    {
        if (root.TryGetProperty("format", out var formatProp) &&
            formatProp.ValueKind == JsonValueKind.String &&
            (formatProp.GetString()?.StartsWith("CrossBusExplorer", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return true;
        }

        if (root.TryGetProperty("crossBusExplorerFormat", out _))
        {
            return true;
        }

        var hasStringBody = root.TryGetProperty("body", out var bodyProp) &&
                            bodyProp.ValueKind == JsonValueKind.String;
        var hasAppProps = root.TryGetProperty("applicationProperties", out var appProps) &&
                          appProps.ValueKind is JsonValueKind.Object or JsonValueKind.Null;

        return hasStringBody && hasAppProps;
    }

    private static string GetFileExtension(Message message, UploadFileType fileType)
    {
        if (fileType == UploadFileType.BodyWithApplicationProperties)
        {
            return ".json";
        }

        if (message.SystemProperties.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ".json";
        }

        return ".txt";
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var ch in value)
        {
            builder.Append(invalidChars.Contains(ch) || ch is '/' or '\\' or ':' ? '_' : ch);
        }

        var sanitized = builder.ToString().Trim();
        return string.IsNullOrEmpty(sanitized) ? "message" : sanitized;
    }

    private static string GetUniqueFilePath(string folderPath, string baseName, string extension)
    {
        var candidate = Path.Combine(folderPath, $"{baseName}{extension}");
        var suffix = 1;

        while (File.Exists(candidate))
        {
            candidate = Path.Combine(folderPath, $"{baseName}_{suffix}{extension}");
            suffix++;
        }

        return candidate;
    }
}
