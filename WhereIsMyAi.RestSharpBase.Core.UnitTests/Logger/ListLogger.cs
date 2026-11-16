using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace WhereIsMyAi.RestSharpBase.Core.UnitTests.Logger;

/// <summary>Test logger: keeps every message as "Level: text" so tests can assert what was logged and at which level.</summary>
internal sealed class ListLogger : ILogger
{
    public List<string> Messages { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) => Messages.Add($"{logLevel}: {formatter(state, exception)}");
}
