using CrossBusExplorer.ServiceBus.Contracts;

namespace CrossBusExplorer.Host.Diagnostics;

public static class ExceptionHints
{
    private static readonly IReadOnlyList<Func<Exception, string?>> Rules =
    [
        exception => exception is ServiceBusOperationException { Code: "ServiceCommunicationProblem" }
            ? "AMQP over TCP (port 5671) may be blocked. Try Transport \"AMQP over WebSockets\" in the connection settings."
            : null
    ];

    public static string? Find(Exception exception) =>
        Rules.Select(rule => rule(exception)).FirstOrDefault(hint => hint is not null);

    public static string Format(string message, string? hint) =>
        hint is null ? message : $"Hint: {hint}\nError: {message}";
}
