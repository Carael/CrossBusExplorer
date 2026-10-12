using CrossBusExplorer.Host.Tracking;
using CrossBusExplorer.ServiceBus.Contracts.Types;

namespace CrossBusExplorer.Tests;

public sealed class TrackingTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyDictionary<string, DateTimeOffset> NoOwnAccess =
        new Dictionary<string, DateTimeOffset>();

    [Fact]
    public void ActivityDiffIgnoresNewAndUnchangedEntities()
    {
        var previous = Snapshots(Queue("orders", 1, 0, T0));
        var current = new[] { Queue("orders", 1, 0, T0), Queue("new", 5, 0, T0) };

        Assert.Empty(ActivityDiff.Compute(previous, current, NoOwnAccess));
    }

    [Fact]
    public void ActivityDiffReportsCountDeltasAndAccess()
    {
        var previous = Snapshots(Queue("orders", 1, 0, T0), Queue("billing", 0, 2, T0));
        var current = new[]
        {
            Queue("orders", 3, 0, T0),
            Queue("billing", 0, 2, T0.AddSeconds(3))
        };

        var changes = ActivityDiff.Compute(previous, current, NoOwnAccess)
            .ToDictionary(change => change.Entity.Path);

        Assert.Equal(2, changes["orders"].ActiveDelta);
        Assert.Equal(0, changes["billing"].ActiveDelta);
        Assert.Equal(0, changes["billing"].DeadLetterDelta);
    }

    [Fact]
    public void ActivityDiffIgnoresAccessCausedByTracker()
    {
        var previous = Snapshots(Queue("orders", 0, 0, T0));
        var current = new[] { Queue("orders", 0, 0, T0.AddSeconds(2)) };
        var ownAccess = new Dictionary<string, DateTimeOffset> { ["orders"] = T0.AddSeconds(5) };

        Assert.Empty(ActivityDiff.Compute(previous, current, ownAccess));
    }

    [Fact]
    public void BufferDeduplicatesAndReturnsItemsAfterCursor()
    {
        var buffer = new TrackingBuffer();

        Assert.True(Add(buffer, "orders", 1));
        Assert.False(Add(buffer, "orders", 1));
        Assert.True(Add(buffer, "orders", 2));
        buffer.AddActivity(new ActivityChange(Queue("orders", 2, 0, T0), 2, 0), T0);

        var all = buffer.GetSince(0, 100);
        Assert.Equal(2, all.Messages.Count);
        Assert.Single(all.Events);
        Assert.Equal(3, all.NextCursor);
        Assert.False(all.HasMore);

        var entity = Assert.Single(all.Entities);
        Assert.Equal(2, entity.CapturedCount);
        Assert.Equal(1, entity.ChangeCount);

        var later = buffer.GetSince(all.NextCursor, 100);
        Assert.Empty(later.Messages);
        Assert.Empty(later.Events);
        Assert.Equal(3, later.NextCursor);
    }

    [Fact]
    public void BufferPagesLargeUpdates()
    {
        var buffer = new TrackingBuffer();
        for (var sequence = 1; sequence <= 5; sequence++)
        {
            Add(buffer, "orders", sequence);
        }

        var first = buffer.GetSince(0, 2);
        Assert.True(first.HasMore);
        Assert.Equal(2, first.Messages.Count);
        Assert.Equal(2, first.NextCursor);

        var rest = buffer.GetSince(first.NextCursor, 10);
        Assert.False(rest.HasMore);
        Assert.Equal([3L, 4L, 5L], rest.Messages.Select(message => message.Cursor));
    }

    [Fact]
    public void BufferEvictsOldestMessagesAndAllowsTheirKeysAgain()
    {
        var buffer = new TrackingBuffer(maxMessages: 2);
        Add(buffer, "orders", 1);
        Add(buffer, "orders", 2);
        Add(buffer, "orders", 3);

        var result = buffer.GetSince(0, 10);
        Assert.Equal([2L, 3L], result.Messages.Select(m => m.Message.SystemProperties.SequenceNumber));
        Assert.Equal(1, buffer.DroppedCount);
        Assert.True(Add(buffer, "orders", 1));
    }

    [Fact]
    public void TapNameIsShortAndRecognizable()
    {
        var name = TrackingSession.CreateTapName(Guid.NewGuid());

        Assert.StartsWith(TrackingSession.TapPrefix, name);
        Assert.True(name.Length <= 50);
    }

    private static bool Add(TrackingBuffer buffer, string path, long sequenceNumber) =>
        buffer.AddMessage(
            path,
            TrackedEntityKind.Queue,
            path,
            null,
            TrackedMessageSource.Peek,
            Message(sequenceNumber),
            T0);

    private static EntitySnapshot Queue(
        string name,
        long active,
        long deadLetter,
        DateTimeOffset accessedAt) =>
        new(name, TrackedEntityKind.Queue, name, null, active, deadLetter, accessedAt);

    private static Dictionary<string, EntitySnapshot> Snapshots(params EntitySnapshot[] items) =>
        items.ToDictionary(item => item.Path);

    private static Message Message(long sequenceNumber) =>
        new(
            $"id-{sequenceNumber}",
            null,
            "{}",
            new MessageSystemProperties(
                null, null, null, null, null, 0, sequenceNumber, T0, T0, T0, string.Empty,
                null, null, null, null, null, sequenceNumber, null, null, TimeSpan.Zero, null),
            null);
}
