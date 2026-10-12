using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus;
using CrossBusExplorer.ServiceBus.Mappings;

namespace CrossBusExplorer.Host.Tracking;

public sealed class TrackingSession : IAsyncDisposable
{
    public const string TapPrefix = "cbe-tap-";
    private const int PeekBatchSize = 250;
    private const int MaxPeekPagesPerCycle = 4;
    private static readonly TimeSpan OwnAccessTolerance = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan BaselineClockSkew = TimeSpan.FromSeconds(5);
    // Short lifetimes keep an abandoned tap from filling the topic quota.
    private static readonly TimeSpan TapIdleLifetime = TimeSpan.FromMinutes(5);

    private readonly object _sync = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly ServiceBusConnection _connection;
    private readonly IServiceBusClientFactory _clientFactory;
    private readonly TrackingBuffer _buffer = new();
    private readonly ConcurrentDictionary<string, long> _lastSequence = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _ownAccessUntil = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TapHandle> _taps = new(StringComparer.Ordinal);
    private ServiceBusAdministrationClient? _administrationClient;
    private Task _runTask = Task.CompletedTask;
    private TrackingSessionStatus _status = TrackingSessionStatus.Starting;
    private string? _error;
    private DateTimeOffset? _stoppedAt;
    private DateTimeOffset? _lastPollAt;
    private int _watchedEntityCount;
    private bool _tapsUnauthorized;

    public TrackingSession(
        Guid id,
        TrackingOptions options,
        ServiceBusConnection connection,
        IServiceBusClientFactory clientFactory)
    {
        Id = id;
        Options = options;
        _connection = connection;
        _clientFactory = clientFactory;
        StartedAt = DateTimeOffset.UtcNow;
        TapSubscriptionName = CreateTapName(id);
    }

    public Guid Id { get; }
    public TrackingOptions Options { get; }
    public DateTimeOffset StartedAt { get; }
    public string TapSubscriptionName { get; }

    public static string CreateTapName(Guid sessionId) =>
        TapPrefix + sessionId.ToString("N")[..8];

    public void Start() => _runTask = Task.Run(RunAsync, CancellationToken.None);

    public TrackingSessionSnapshot Snapshot()
    {
        lock (_sync)
        {
            return new TrackingSessionSnapshot(
                Id,
                Options.ConnectionName,
                Options.NameFilter,
                Options.EnableTopicTaps,
                Options.IncludeDeadLetter,
                (int)Options.PollInterval.TotalSeconds,
                _status,
                _error,
                StartedAt,
                _stoppedAt,
                _lastPollAt,
                _watchedEntityCount,
                _buffer.DroppedCount,
                _taps.Values
                    .Select(tap => tap.ToStatus())
                    .OrderBy(tap => tap.TopicName, StringComparer.OrdinalIgnoreCase)
                    .ToList());
        }
    }

    public TrackingUpdates GetUpdates(long afterCursor, int maxItems)
    {
        var (messages, events, entities, nextCursor, hasMore) =
            _buffer.GetSince(afterCursor, maxItems);
        return new TrackingUpdates(Snapshot(), messages, events, entities, nextCursor, hasMore);
    }

    public async Task StopAsync()
    {
        if (!_cancellation.IsCancellationRequested)
        {
            await _cancellation.CancelAsync();
        }

        try
        {
            await _runTask;
        }
        catch (OperationCanceledException)
        {
        }

        await RemoveTapsAsync();

        lock (_sync)
        {
            if (_status != TrackingSessionStatus.Failed)
            {
                _status = TrackingSessionStatus.Stopped;
            }
            _stoppedAt ??= DateTimeOffset.UtcNow;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cancellation.Dispose();
    }

    private async Task RunAsync()
    {
        var cancellationToken = _cancellation.Token;

        try
        {
            _administrationClient = _clientFactory.GetAdministrationClient(_connection);
            var client = _clientFactory.GetClient(_connection);
            SetStatus(TrackingSessionStatus.Running, null);

            Dictionary<string, EntitySnapshot>? previous = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var current = await LoadEntitiesAsync(_administrationClient, cancellationToken);

                    if (Options.EnableTopicTaps)
                    {
                        await EnsureTapsAsync(
                            _administrationClient,
                            client,
                            current.Where(entity => entity.Kind == TrackedEntityKind.Topic),
                            cancellationToken);
                    }

                    if (previous is not null)
                    {
                        var changes = ActivityDiff.Compute(
                            previous,
                            current,
                            new Dictionary<string, DateTimeOffset>(_ownAccessUntil));
                        var now = DateTimeOffset.UtcNow;
                        foreach (var change in changes)
                        {
                            _buffer.AddActivity(change, now);
                        }

                        await PeekChangedAsync(client, changes, cancellationToken);
                    }

                    previous = current.ToDictionary(entity => entity.Path, StringComparer.Ordinal);

                    lock (_sync)
                    {
                        _lastPollAt = DateTimeOffset.UtcNow;
                        _watchedEntityCount = current.Count;
                        _error = null;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    SetError(exception.Message);
                }

                await Task.Delay(Options.PollInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            SetStatus(TrackingSessionStatus.Failed, exception.Message);
        }
    }

    private async Task<List<EntitySnapshot>> LoadEntitiesAsync(
        ServiceBusAdministrationClient administrationClient,
        CancellationToken cancellationToken)
    {
        var entities = new ConcurrentBag<EntitySnapshot>();

        await foreach (var queue in administrationClient.GetQueuesRuntimePropertiesAsync(
                           cancellationToken))
        {
            if (Matches(queue.Name))
            {
                entities.Add(new EntitySnapshot(
                    queue.Name,
                    TrackedEntityKind.Queue,
                    queue.Name,
                    null,
                    queue.ActiveMessageCount,
                    queue.DeadLetterMessageCount,
                    queue.AccessedAt));
            }
        }

        var topics = new List<TopicRuntimeProperties>();
        await foreach (var topic in administrationClient.GetTopicsRuntimePropertiesAsync(
                           cancellationToken))
        {
            if (Matches(topic.Name))
            {
                topics.Add(topic);
                entities.Add(new EntitySnapshot(
                    topic.Name,
                    TrackedEntityKind.Topic,
                    topic.Name,
                    null,
                    0,
                    0,
                    topic.AccessedAt));
            }
        }

        await Parallel.ForEachAsync(
            topics,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 8,
                CancellationToken = cancellationToken
            },
            async (topic, token) =>
            {
                await foreach (var subscription in
                               administrationClient.GetSubscriptionsRuntimePropertiesAsync(
                                   topic.Name,
                                   token))
                {
                    if (subscription.SubscriptionName.StartsWith(
                            TapPrefix,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    entities.Add(new EntitySnapshot(
                        SubscriptionPath(topic.Name, subscription.SubscriptionName),
                        TrackedEntityKind.Subscription,
                        topic.Name,
                        subscription.SubscriptionName,
                        subscription.ActiveMessageCount,
                        subscription.DeadLetterMessageCount,
                        subscription.AccessedAt));
                }
            });

        return entities.ToList();
    }

    private async Task PeekChangedAsync(
        ServiceBusClient client,
        IReadOnlyList<ActivityChange> changes,
        CancellationToken cancellationToken)
    {
        var work = new List<(EntitySnapshot Entity, bool DeadLetter, long Delta)>();

        foreach (var change in changes)
        {
            var entity = change.Entity;
            if (entity.Kind == TrackedEntityKind.Topic)
            {
                continue;
            }

            var topicIsTapped = entity.Kind == TrackedEntityKind.Subscription &&
                                _taps.TryGetValue(entity.EntityName, out var tap) &&
                                tap.State == TapState.Active;
            if (!topicIsTapped)
            {
                work.Add((entity, false, change.ActiveDelta));
            }

            if (Options.IncludeDeadLetter && change.DeadLetterDelta > 0)
            {
                work.Add((entity, true, change.DeadLetterDelta));
            }
        }

        await Parallel.ForEachAsync(
            work,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,
                CancellationToken = cancellationToken
            },
            async (item, token) =>
            {
                try
                {
                    await PeekAsync(client, item.Entity, item.DeadLetter, item.Delta, token);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    SetError($"{item.Entity.Path}: {exception.Message}");
                }
                finally
                {
                    _ownAccessUntil[item.Entity.Path] = DateTimeOffset.UtcNow + OwnAccessTolerance;
                }
            });
    }

    private async Task PeekAsync(
        ServiceBusClient client,
        EntitySnapshot entity,
        bool deadLetter,
        long delta,
        CancellationToken cancellationToken)
    {
        var options = new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            SubQueue = deadLetter ? SubQueue.DeadLetter : SubQueue.None
        };

        await using var receiver = entity.SubscriptionName is null
            ? client.CreateReceiver(entity.EntityName, options)
            : client.CreateReceiver(entity.EntityName, entity.SubscriptionName, options);

        var key = $"{entity.Path}|{(deadLetter ? "dlq" : "active")}";
        var isBaseline = !_lastSequence.TryGetValue(key, out var lastSequence);
        long? fromSequence = isBaseline ? null : lastSequence + 1;
        var source = deadLetter ? TrackedMessageSource.DeadLetter : TrackedMessageSource.Peek;
        var baselineCandidates = new List<ServiceBusReceivedMessage>();

        for (var page = 0; page < MaxPeekPagesPerCycle; page++)
        {
            var batch = await receiver.PeekMessagesAsync(
                PeekBatchSize,
                fromSequence,
                cancellationToken);

            foreach (var message in batch)
            {
                lastSequence = Math.Max(lastSequence, message.SequenceNumber);

                if (isBaseline)
                {
                    baselineCandidates.Add(message);
                }
                else
                {
                    Capture(entity, source, message);
                }
            }

            if (batch.Count < PeekBatchSize)
            {
                break;
            }

            fromSequence = batch[^1].SequenceNumber + 1;
        }

        if (isBaseline)
        {
            // Without a known sequence position, only messages that arrived after the
            // session started count; dead-letter times reflect the original enqueue.
            var threshold = StartedAt - BaselineClockSkew;
            var recent = baselineCandidates.Where(message => message.EnqueuedTime >= threshold);
            if (deadLetter && delta > 0)
            {
                recent = recent.Union(baselineCandidates.TakeLast((int)Math.Min(delta, PeekBatchSize)));
            }

            foreach (var message in recent)
            {
                Capture(entity, source, message);
            }
        }

        if (lastSequence > 0 || !isBaseline)
        {
            _lastSequence[key] = lastSequence;
        }
    }

    private async Task EnsureTapsAsync(
        ServiceBusAdministrationClient administrationClient,
        ServiceBusClient client,
        IEnumerable<EntitySnapshot> topics,
        CancellationToken cancellationToken)
    {
        foreach (var topic in topics)
        {
            if (_tapsUnauthorized || _taps.ContainsKey(topic.EntityName))
            {
                continue;
            }

            var tap = new TapHandle(topic.EntityName, TapSubscriptionName);
            _taps[topic.EntityName] = tap;

            try
            {
                await administrationClient.CreateSubscriptionAsync(
                    new CreateSubscriptionOptions(topic.EntityName, TapSubscriptionName)
                    {
                        AutoDeleteOnIdle = TapIdleLifetime,
                        DefaultMessageTimeToLive = TapIdleLifetime,
                        MaxDeliveryCount = 1,
                        UserMetadata = "Cross Bus Explorer live tracker (temporary)"
                    },
                    cancellationToken);
                tap.Created = true;
            }
            catch (ServiceBusException exception) when
                (exception.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
            {
            }
            catch (UnauthorizedAccessException exception)
            {
                tap.Fail(TapState.Unauthorized, exception.Message);
                _tapsUnauthorized = true;
                continue;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                tap.Fail(TapState.Failed, exception.Message);
                continue;
            }

            var processor = client.CreateProcessor(
                topic.EntityName,
                TapSubscriptionName,
                new ServiceBusProcessorOptions
                {
                    ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
                    MaxConcurrentCalls = 1,
                    PrefetchCount = 50
                });
            processor.ProcessMessageAsync += args =>
            {
                Capture(
                    new EntitySnapshot(
                        topic.EntityName,
                        TrackedEntityKind.Topic,
                        topic.EntityName,
                        null,
                        0,
                        0,
                        DateTimeOffset.UtcNow),
                    TrackedMessageSource.Tap,
                    args.Message);
                return Task.CompletedTask;
            };
            processor.ProcessErrorAsync += args =>
            {
                if (args.Exception is UnauthorizedAccessException)
                {
                    tap.Fail(TapState.Unauthorized, args.Exception.Message);
                }
                else
                {
                    // The processor retries transient failures on its own.
                    tap.SetError(args.Exception.Message);
                }
                return Task.CompletedTask;
            };

            tap.Processor = processor;
            try
            {
                await processor.StartProcessingAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                tap.Fail(TapState.Failed, exception.Message);
                continue;
            }

            tap.Activate();
            // Tap receives touch the topic, so its AccessedAt is no longer a useful signal.
            _ownAccessUntil[topic.Path] = DateTimeOffset.MaxValue;
        }
    }

    private async Task RemoveTapsAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        await Parallel.ForEachAsync(
            _taps.Values,
            new ParallelOptions { MaxDegreeOfParallelism = 8 },
            async (tap, _) =>
            {
                try
                {
                    if (tap.Processor is not null)
                    {
                        await tap.Processor.StopProcessingAsync(timeout.Token);
                        await tap.Processor.DisposeAsync();
                        tap.Processor = null;
                    }

                    if (tap.Created && _administrationClient is not null)
                    {
                        await _administrationClient.DeleteSubscriptionAsync(
                            tap.TopicName,
                            tap.SubscriptionName,
                            timeout.Token);
                        tap.Created = false;
                    }
                }
                catch (Exception)
                {
                    // AutoDeleteOnIdle removes taps that could not be deleted here.
                }
            });
    }

    private void Capture(
        EntitySnapshot entity,
        TrackedMessageSource source,
        ServiceBusReceivedMessage message)
    {
        try
        {
            _buffer.AddMessage(
                entity.Path,
                entity.Kind,
                entity.EntityName,
                entity.SubscriptionName,
                source,
                message.MapToMessage(),
                DateTimeOffset.UtcNow);
        }
        catch (NotSupportedException exception)
        {
            SetError($"{entity.Path}: {exception.Message}");
        }
    }

    private bool Matches(string name) =>
        string.IsNullOrWhiteSpace(Options.NameFilter) ||
        name.Contains(Options.NameFilter.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string SubscriptionPath(string topicName, string subscriptionName) =>
        $"{topicName}/subscriptions/{subscriptionName}";

    private void SetStatus(TrackingSessionStatus status, string? error)
    {
        lock (_sync)
        {
            _status = status;
            _error = error;
            if (status == TrackingSessionStatus.Failed)
            {
                _stoppedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    private void SetError(string error)
    {
        lock (_sync)
        {
            _error = error;
        }
    }

    private sealed class TapHandle
    {
        private readonly object _sync = new();
        private TapState _state = TapState.Creating;
        private string? _error;

        public TapHandle(string topicName, string subscriptionName)
        {
            TopicName = topicName;
            SubscriptionName = subscriptionName;
        }

        public string TopicName { get; }
        public string SubscriptionName { get; }
        public bool Created { get; set; }
        public ServiceBusProcessor? Processor { get; set; }

        public TapState State
        {
            get
            {
                lock (_sync)
                {
                    return _state;
                }
            }
        }

        public void Activate()
        {
            lock (_sync)
            {
                if (_state == TapState.Creating)
                {
                    _state = TapState.Active;
                }
            }
        }

        public void Fail(TapState state, string error)
        {
            lock (_sync)
            {
                _state = state;
                _error = error;
            }
        }

        public void SetError(string error)
        {
            lock (_sync)
            {
                _error = error;
            }
        }

        public TapStatus ToStatus()
        {
            lock (_sync)
            {
                return new TapStatus(TopicName, SubscriptionName, _state, _error);
            }
        }
    }
}
