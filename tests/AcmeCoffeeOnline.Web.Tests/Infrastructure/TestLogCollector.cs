using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace AcmeCoffeeOnline.Web.Tests.Infrastructure;

internal sealed class TestLogCollector
{
    private readonly ConcurrentQueue<TestLogEntry> _entries = new();

    public IReadOnlyList<TestLogEntry> Entries => _entries.ToArray();

    public ILoggerProvider CreateProvider()
    {
        return new TestLoggerProvider(_entries);
    }
}

internal sealed record TestLogEntry(
    string Category,
    LogLevel Level,
    EventId EventId,
    string Message,
    IReadOnlyDictionary<string, object?> State);

internal sealed class TestLoggerProvider(
    ConcurrentQueue<TestLogEntry> entries) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(categoryName, entries);
    }

    public void Dispose()
    {
    }
}

internal sealed class TestLogger(
    string categoryName,
    ConcurrentQueue<TestLogEntry> entries) : ILogger
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var structuredState = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (state is IEnumerable<KeyValuePair<string, object?>> values)
        {
            foreach (var pair in values)
            {
                structuredState[pair.Key] = pair.Value;
            }
        }

        entries.Enqueue(new TestLogEntry(
            categoryName,
            logLevel,
            eventId,
            formatter(state, exception),
            structuredState));
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
