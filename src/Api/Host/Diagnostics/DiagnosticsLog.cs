namespace CrossBusExplorer.Host.Diagnostics;

public sealed record DiagnosticsEntry(
    long Id,
    DateTimeOffset Timestamp,
    string Level,
    string Source,
    string Message,
    string? Details);

public sealed class DiagnosticsLog
{
    private const int Capacity = 1000;

    private readonly object _gate = new();
    private readonly LinkedList<DiagnosticsEntry> _entries = new();
    private long _nextId;

    public void Add(string level, string source, string message, string? details = null)
    {
        lock (_gate)
        {
            _entries.AddLast(new DiagnosticsEntry(
                ++_nextId,
                DateTimeOffset.UtcNow,
                level,
                source,
                message,
                details));

            while (_entries.Count > Capacity)
            {
                _entries.RemoveFirst();
            }
        }
    }

    public IReadOnlyList<DiagnosticsEntry> Snapshot()
    {
        lock (_gate)
        {
            return _entries.ToList();
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _entries.Clear();
        }
    }
}
