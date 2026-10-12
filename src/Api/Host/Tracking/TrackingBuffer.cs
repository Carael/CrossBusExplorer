using CrossBusExplorer.ServiceBus.Contracts.Types;

namespace CrossBusExplorer.Host.Tracking;

public sealed class TrackingBuffer
{
    private readonly object _sync = new();
    private readonly int _maxMessages;
    private readonly int _maxEvents;
    private readonly long _maxBodyChars;
    private readonly Queue<TrackedMessage> _messages = new();
    private readonly Queue<ActivityEvent> _events = new();
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EntityActivity> _entities = new(StringComparer.Ordinal);
    private long _cursor;
    private long _bodyChars;
    private long _dropped;

    public TrackingBuffer(int maxMessages = 5000, int maxEvents = 5000, long maxBodyChars = 64_000_000)
    {
        _maxMessages = maxMessages;
        _maxEvents = maxEvents;
        _maxBodyChars = maxBodyChars;
    }

    public long DroppedCount
    {
        get
        {
            lock (_sync)
            {
                return _dropped;
            }
        }
    }

    public bool AddMessage(
        string entityPath,
        TrackedEntityKind kind,
        string entityName,
        string? subscriptionName,
        TrackedMessageSource source,
        Message message,
        DateTimeOffset capturedAt)
    {
        lock (_sync)
        {
            if (!_seen.Add(DedupKey(entityPath, source, message.SystemProperties.SequenceNumber)))
            {
                return false;
            }

            var tracked = new TrackedMessage(
                ++_cursor,
                capturedAt,
                entityPath,
                kind,
                entityName,
                subscriptionName,
                source,
                message);
            _messages.Enqueue(tracked);
            _bodyChars += message.Body.Length;

            while (_messages.Count > _maxMessages ||
                   (_bodyChars > _maxBodyChars && _messages.Count > 1))
            {
                var evicted = _messages.Dequeue();
                _bodyChars -= evicted.Message.Body.Length;
                _seen.Remove(DedupKey(
                    evicted.EntityPath,
                    evicted.Source,
                    evicted.Message.SystemProperties.SequenceNumber));
                _dropped++;
            }

            var entity = GetOrCreateEntity(entityPath, kind, entityName, subscriptionName, capturedAt);
            _entities[entityPath] = entity with
            {
                LastActivityAt = Max(entity.LastActivityAt, capturedAt),
                CapturedCount = entity.CapturedCount + 1
            };

            return true;
        }
    }

    public void AddActivity(ActivityChange change, DateTimeOffset at)
    {
        var snapshot = change.Entity;

        lock (_sync)
        {
            _events.Enqueue(new ActivityEvent(
                ++_cursor,
                at,
                snapshot.Path,
                snapshot.Kind,
                change.ActiveDelta,
                change.DeadLetterDelta,
                snapshot.ActiveCount,
                snapshot.DeadLetterCount));

            while (_events.Count > _maxEvents)
            {
                _events.Dequeue();
                _dropped++;
            }

            var entity = GetOrCreateEntity(
                snapshot.Path,
                snapshot.Kind,
                snapshot.EntityName,
                snapshot.SubscriptionName,
                at);
            _entities[snapshot.Path] = entity with
            {
                LastActivityAt = Max(entity.LastActivityAt, at),
                ActiveCount = snapshot.ActiveCount,
                DeadLetterCount = snapshot.DeadLetterCount,
                ChangeCount = entity.ChangeCount + 1
            };
        }
    }

    public (IReadOnlyList<TrackedMessage> Messages,
        IReadOnlyList<ActivityEvent> Events,
        IReadOnlyList<EntityActivity> Entities,
        long NextCursor,
        bool HasMore) GetSince(long afterCursor, int maxItems)
    {
        lock (_sync)
        {
            var messages = _messages.Where(item => item.Cursor > afterCursor).ToList();
            var events = _events.Where(item => item.Cursor > afterCursor).ToList();
            var hasMore = messages.Count + events.Count > maxItems;

            if (hasMore)
            {
                var lastCursor = messages.Select(item => item.Cursor)
                    .Concat(events.Select(item => item.Cursor))
                    .Order()
                    .Take(maxItems)
                    .Last();
                messages = messages.Where(item => item.Cursor <= lastCursor).ToList();
                events = events.Where(item => item.Cursor <= lastCursor).ToList();
            }

            var nextCursor = hasMore
                ? Math.Max(
                    messages.Count > 0 ? messages[^1].Cursor : afterCursor,
                    events.Count > 0 ? events[^1].Cursor : afterCursor)
                : Math.Max(afterCursor, _cursor);

            return (
                messages,
                events,
                _entities.Values.OrderByDescending(item => item.LastActivityAt).ToList(),
                nextCursor,
                hasMore);
        }
    }

    private EntityActivity GetOrCreateEntity(
        string path,
        TrackedEntityKind kind,
        string entityName,
        string? subscriptionName,
        DateTimeOffset at) =>
        _entities.TryGetValue(path, out var existing)
            ? existing
            : new EntityActivity(path, kind, entityName, subscriptionName, at, 0, 0, 0, 0);

    private static string DedupKey(string path, TrackedMessageSource source, long sequenceNumber) =>
        $"{source}|{path}|{sequenceNumber}";

    private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right) =>
        left > right ? left : right;
}
