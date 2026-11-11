namespace CrossBusExplorer.Host.Diagnostics;

public sealed class DiagnosticsLoggerProvider(DiagnosticsLog log) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new DiagnosticsLogger(log, categoryName);

    public void Dispose()
    {
    }

    private sealed class DiagnosticsLogger(DiagnosticsLog log, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            log.Add(
                logLevel >= LogLevel.Error ? "error" : "warning",
                category,
                formatter(state, exception),
                exception?.ToString());
        }
    }
}
