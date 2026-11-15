using System;
using Microsoft.Extensions.Logging;

namespace WhereIsMyAi.RestSharpBase.Core.Logger;

/// <summary>Single shared logger built on Microsoft.Extensions.Logging; writes to the console (captured by the test runner).</summary>
public static class InternalLogger
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(builder => builder
        .SetMinimumLevel(LogLevel.Information)
        .AddProvider(new SyncConsoleLoggerProvider()));

    public static ILogger Logger { get; } = Factory.CreateLogger("RestSharpBase");

    // Not AddConsole: it writes from a background thread, so the runner attaches every line to the first test.
    private sealed class SyncConsoleLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new SyncConsoleLogger(categoryName);

        public void Dispose()
        {
        }
    }

    private sealed class SyncConsoleLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            Console.WriteLine($"[{logLevel}] {category}: {formatter(state, exception)}");
            if (exception != null)
                Console.WriteLine(exception);
        }
    }
}
