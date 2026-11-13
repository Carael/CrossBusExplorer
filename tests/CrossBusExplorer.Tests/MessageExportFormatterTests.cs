using System.Text.Json;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;

namespace CrossBusExplorer.Tests;

public sealed class MessageExportFormatterTests
{
    [Fact]
    public void FormatAndParseMessage_RoundTripsCrossBusExplorerFormat()
    {
        var original = CreateSampleMessage("msg-42", 42, """{"orderId":1001}""");

        var serialized = MessageExportFormatter.FormatMessage(
            original,
            UploadFileType.BodyWithApplicationProperties);

        var parsed = MessageExportFormatter.ParseMessage(serialized);

        Assert.Equal("""{"orderId":1001}""", parsed.Body);
        Assert.Equal("msg-42", parsed.Id);
        Assert.Equal("OrderCreated", parsed.Subject);
        Assert.Equal("application/json", parsed.ContentType);
        Assert.Equal("corr-1", parsed.CorrelationId);
        Assert.NotNull(parsed.ApplicationProperties);
        Assert.True(parsed.ApplicationProperties!.ContainsKey("tenant"));
        var tenantValue = parsed.ApplicationProperties["tenant"];
        Assert.Equal("acme", tenantValue is JsonElement je ? je.GetString() : tenantValue?.ToString());
    }

    [Fact]
    public void ParseMessage_WhenNotCrossBusExplorerFormat_SetsBodyDirectly()
    {
        const string plainJson = """{"customer":"Jane","amount":120.5}""";

        var parsed = MessageExportFormatter.ParseMessage(plainJson);

        Assert.Equal(plainJson, parsed.Body);
        Assert.Null(parsed.ApplicationProperties);
        Assert.Null(parsed.Subject);
    }

    [Fact]
    public async Task ExportMessagesToFolderAsync_WritesSeparateFilesForEachMessage()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), $"cbe-export-test-{Guid.NewGuid():N}");

        try
        {
            var messages = new[]
            {
                CreateSampleMessage("msg/1", 10, "first body"),
                CreateSampleMessage("msg/2", 11, "second body")
            };

            var count = await MessageExportFormatter.ExportMessagesToFolderAsync(
                tempFolder,
                "orders-queue",
                messages,
                UploadFileType.BodyWithApplicationProperties,
                CancellationToken.None);

            Assert.Equal(2, count);
            var files = Directory.GetFiles(tempFolder).OrderBy(f => f).ToArray();
            Assert.Equal(2, files.Length);

            var firstParsed = MessageExportFormatter.ParseMessage(await File.ReadAllTextAsync(files[0]));
            var secondParsed = MessageExportFormatter.ParseMessage(await File.ReadAllTextAsync(files[1]));

            Assert.Equal("first body", firstParsed.Body);
            Assert.Equal("second body", secondParsed.Body);
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, recursive: true);
            }
        }
    }

    private static Message CreateSampleMessage(string id, long sequenceNumber, string body) =>
        new(
            id,
            "OrderCreated",
            body,
            new MessageSystemProperties(
                ContentType: "application/json",
                CorrelationId: "corr-1",
                DeadLetterSource: null,
                DeadLetterReason: null,
                DeadLetterErrorDescription: null,
                DeliveryCount: 1,
                EnqueuedSequenceNumber: sequenceNumber,
                EnqueuedTime: DateTimeOffset.UtcNow,
                ExpiresAt: DateTimeOffset.UtcNow.AddDays(1),
                LockedUntil: DateTimeOffset.UtcNow.AddMinutes(1),
                LockToken: Guid.NewGuid().ToString(),
                PartitionKey: "p1",
                TransactionPartitionKey: null,
                ReplyTo: "reply-queue",
                ReplyToSessionId: null,
                ScheduledEnqueueTime: DateTimeOffset.MinValue,
                SequenceNumber: sequenceNumber,
                SessionId: "s1",
                State: MessageState.Active,
                TimeToLive: TimeSpan.FromDays(1),
                To: "orders"),
            new Dictionary<string, object?>
            {
                ["tenant"] = "acme",
                ["priority"] = 2
            });
}
