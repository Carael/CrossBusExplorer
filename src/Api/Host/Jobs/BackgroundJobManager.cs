using System.Collections.Concurrent;

namespace CrossBusExplorer.Host.Jobs;

public enum BackgroundJobStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Cancelled
}

public sealed record BackgroundJobSnapshot(
    Guid Id,
    string Name,
    BackgroundJobStatus Status,
    int Progress,
    string? Message,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed class BackgroundJobManager
{
    private readonly ConcurrentDictionary<Guid, BackgroundJobState> _jobs = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public BackgroundJobManager(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public BackgroundJobSnapshot Start(
        string name,
        Func<IServiceProvider, CancellationToken, IProgress<int>, Task<string?>> operation)
    {
        var state = new BackgroundJobState(Guid.NewGuid(), name);
        _jobs[state.Id] = state;

        _ = Task.Run(() => RunAsync(state, operation), CancellationToken.None);
        return state.Snapshot();
    }

    public IReadOnlyList<BackgroundJobSnapshot> GetAll() =>
        _jobs.Values
            .OrderByDescending(job => job.CreatedAt)
            .Select(job => job.Snapshot())
            .ToList();

    public BackgroundJobSnapshot? Get(Guid id) =>
        _jobs.TryGetValue(id, out var state) ? state.Snapshot() : null;

    public bool Cancel(Guid id)
    {
        if (!_jobs.TryGetValue(id, out var state) || state.IsTerminal)
        {
            return false;
        }

        state.Cancel();
        return true;
    }

    private async Task RunAsync(
        BackgroundJobState state,
        Func<IServiceProvider, CancellationToken, IProgress<int>, Task<string?>> operation)
    {
        state.SetRunning();
        using var scope = _scopeFactory.CreateScope();
        var progress = new Progress<int>(state.SetProgress);

        try
        {
            var message = await operation(
                scope.ServiceProvider,
                state.CancellationToken,
                progress);
            state.SetSucceeded(message);
        }
        catch (OperationCanceledException) when (state.CancellationToken.IsCancellationRequested)
        {
            state.SetCancelled();
        }
        catch (Exception exception)
        {
            state.SetFailed(exception.Message);
        }
    }

    private sealed class BackgroundJobState
    {
        private readonly object _sync = new();
        private readonly CancellationTokenSource _cancellation = new();
        private BackgroundJobStatus _status = BackgroundJobStatus.Queued;
        private int _progress;
        private string? _message;
        private DateTimeOffset? _completedAt;

        public BackgroundJobState(Guid id, string name)
        {
            Id = id;
            Name = name;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public Guid Id { get; }
        public string Name { get; }
        public DateTimeOffset CreatedAt { get; }
        public CancellationToken CancellationToken => _cancellation.Token;

        public bool IsTerminal
        {
            get
            {
                lock (_sync)
                {
                    return _status is BackgroundJobStatus.Succeeded
                        or BackgroundJobStatus.Failed
                        or BackgroundJobStatus.Cancelled;
                }
            }
        }

        public void SetRunning()
        {
            lock (_sync)
            {
                _status = BackgroundJobStatus.Running;
            }
        }

        public void SetProgress(int progress)
        {
            lock (_sync)
            {
                _progress = Math.Clamp(progress, 0, 100);
            }
        }

        public void SetSucceeded(string? message)
        {
            lock (_sync)
            {
                _status = BackgroundJobStatus.Succeeded;
                _progress = 100;
                _message = message;
                _completedAt = DateTimeOffset.UtcNow;
            }
        }

        public void SetFailed(string message)
        {
            lock (_sync)
            {
                _status = BackgroundJobStatus.Failed;
                _message = message;
                _completedAt = DateTimeOffset.UtcNow;
            }
        }

        public void SetCancelled()
        {
            lock (_sync)
            {
                _status = BackgroundJobStatus.Cancelled;
                _message = "Cancelled by user.";
                _completedAt = DateTimeOffset.UtcNow;
            }
        }

        public void Cancel() => _cancellation.Cancel();

        public BackgroundJobSnapshot Snapshot()
        {
            lock (_sync)
            {
                return new BackgroundJobSnapshot(
                    Id,
                    Name,
                    _status,
                    _progress,
                    _message,
                    CreatedAt,
                    _completedAt);
            }
        }
    }
}
