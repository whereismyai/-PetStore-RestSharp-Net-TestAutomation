using Microsoft.Extensions.Logging;

namespace WhereIsMyAi.RestSharpBase.Core.Logger;

/// <summary>Single shared logger built on Microsoft.Extensions.Logging; writes to the console (captured by the test runner).</summary>
public static class InternalLogger
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(builder => builder
        .SetMinimumLevel(LogLevel.Information)
        .AddConsole());

    public static ILogger Logger { get; set; } = Factory.CreateLogger("RestSharpBase");
}
