using CrossBusExplorer.ServiceBus.Contracts.Types;

namespace CrossBusExplorer.Host.Tracking;

public enum TrackingSessionStatus
{
    Starting,
    Running,
    Stopped,
    Failed
}

public enum TrackedEntityKind
{
    Queue,
    Topic,
    Subscription
}

public enum TrackedMessageSource
{
    Tap,
    Peek,
    DeadLetter
}

public enum TapState
{
    Creating,
    Active,
    Unauthorized,
    Failed
}

public sealed record StartTrackingRequest(
    string ConnectionName,
    string? NameFilter,
    bool EnableTopicTaps,
    bool IncludeDeadLetter,
    int? PollIntervalSeconds);

public sealed record TrackingOptions(
    string ConnectionName,
    string? NameFilter,
    bool EnableTopicTaps,
    bool IncludeDeadLetter,
    TimeSpan PollInterval);

public sealed record EntitySnapshot(
    string Path,
    TrackedEntityKind Kind,
    string EntityName,
    string? SubscriptionName,
    long ActiveCount,
    long DeadLetterCount,
    DateTimeOffset AccessedAt);

public sealed record ActivityChange(
    EntitySnapshot Entity,
    long ActiveDelta,
    long DeadLetterDelta);

public sealed record TrackedMessage(
    long Cursor,
    DateTimeOffset CapturedAt,
    string EntityPath,
    TrackedEntityKind EntityKind,
    string EntityName,
    string? SubscriptionName,
    TrackedMessageSource Source,
    Message Message);

public sealed record ActivityEvent(
    long Cursor,
    DateTimeOffset At,
    string EntityPath,
    TrackedEntityKind EntityKind,
    long ActiveDelta,
    long DeadLetterDelta,
    long ActiveCount,
    long DeadLetterCount);

public sealed record EntityActivity(
    string EntityPath,
    TrackedEntityKind EntityKind,
    string EntityName,
    string? SubscriptionName,
    DateTimeOffset LastActivityAt,
    long ActiveCount,
    long DeadLetterCount,
    int ChangeCount,
    int CapturedCount);

public sealed record TapStatus(
    string TopicName,
    string SubscriptionName,
    TapState State,
    string? Error);

public sealed record TrackingSessionSnapshot(
    Guid Id,
    string ConnectionName,
    string? NameFilter,
    bool EnableTopicTaps,
    bool IncludeDeadLetter,
    int PollIntervalSeconds,
    TrackingSessionStatus Status,
    string? Error,
    DateTimeOffset StartedAt,
    DateTimeOffset? StoppedAt,
    DateTimeOffset? LastPollAt,
    int WatchedEntityCount,
    long DroppedCount,
    IReadOnlyList<TapStatus> Taps);

public sealed record TrackingUpdates(
    TrackingSessionSnapshot Session,
    IReadOnlyList<TrackedMessage> Messages,
    IReadOnlyList<ActivityEvent> Events,
    IReadOnlyList<EntityActivity> Entities,
    long NextCursor,
    bool HasMore);
