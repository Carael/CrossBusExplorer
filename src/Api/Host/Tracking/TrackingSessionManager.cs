using System.Collections.Concurrent;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus;

namespace CrossBusExplorer.Host.Tracking;

public sealed class TrackingSessionManager : IHostedService
{
    public const int DefaultPollIntervalSeconds = 5;
    public const int MinPollIntervalSeconds = 2;
    public const int MaxPollIntervalSeconds = 300;
    private const int MaxItemsPerUpdate = 1000;

    private readonly ConcurrentDictionary<Guid, TrackingSession> _sessions = new();
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceBusClientFactory _clientFactory;

    public TrackingSessionManager(
        IServiceScopeFactory scopeFactory,
        IServiceBusClientFactory clientFactory)
    {
        _scopeFactory = scopeFactory;
        _clientFactory = clientFactory;
    }

    public async Task<TrackingSessionSnapshot> StartSessionAsync(
        StartTrackingRequest request,
        CancellationToken cancellationToken)
    {
        var options = new TrackingOptions(
            request.ConnectionName,
            string.IsNullOrWhiteSpace(request.NameFilter) ? null : request.NameFilter.Trim(),
            request.EnableTopicTaps,
            request.IncludeDeadLetter,
            TimeSpan.FromSeconds(Math.Clamp(
                request.PollIntervalSeconds ?? DefaultPollIntervalSeconds,
                MinPollIntervalSeconds,
                MaxPollIntervalSeconds)));

        ServiceBusConnection connection;
        using (var scope = _scopeFactory.CreateScope())
        {
            connection = await scope.ServiceProvider
                .GetRequiredService<IConnectionManagement>()
                .GetAsync(request.ConnectionName, cancellationToken);
        }

        await _startLock.WaitAsync(cancellationToken);
        try
        {
            // One session per connection keeps tap subscriptions and polling load bounded.
            foreach (var existing in _sessions.Values.Where(session =>
                         session.Options.ConnectionName == request.ConnectionName))
            {
                await RemoveSessionAsync(existing.Id);
            }

            var session = new TrackingSession(Guid.NewGuid(), options, connection, _clientFactory);
            _sessions[session.Id] = session;
            session.Start();
            return session.Snapshot();
        }
        finally
        {
            _startLock.Release();
        }
    }

    public IReadOnlyList<TrackingSessionSnapshot> GetSessions() =>
        _sessions.Values
            .Select(session => session.Snapshot())
            .OrderByDescending(session => session.StartedAt)
            .ToList();

    public TrackingUpdates? GetUpdates(Guid id, long afterCursor) =>
        _sessions.TryGetValue(id, out var session)
            ? session.GetUpdates(Math.Max(0, afterCursor), MaxItemsPerUpdate)
            : null;

    public async Task<TrackingSessionSnapshot?> StopSessionAsync(Guid id)
    {
        if (!_sessions.TryGetValue(id, out var session))
        {
            return null;
        }

        await session.StopAsync();
        return session.Snapshot();
    }

    public async Task<bool> RemoveSessionAsync(Guid id)
    {
        if (!_sessions.TryRemove(id, out var session))
        {
            return false;
        }

        await session.DisposeAsync();
        return true;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_sessions.Values.Select(session => session.StopAsync()))
            .WaitAsync(cancellationToken);
    }
}
