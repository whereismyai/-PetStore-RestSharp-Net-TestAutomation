using System;
using System.Collections.Generic;
using System.IO;

namespace WhereIsMyAi.RestSharpBase.Core.Helpers;

/// <summary>
/// Reads KEY=VALUE settings from a .env file, found by searching upward from the build output
/// folder (so it works from bin/Debug/... and from the repo root). A real environment variable
/// with the same name wins, which lets CI override the file without having one.
/// </summary>
public static class EnvFile
{
    private static readonly Lazy<Dictionary<string, string>> Values = new(Load);

    /// <summary>A required setting; throws when it is missing.</summary>
    public static string Get(string key) =>
        TryGet(key) ?? throw new InvalidOperationException(
            $"Setting '{key}' not found. Add it to a .env file (see .env.example) or set it as an environment variable.");

    /// <summary>An optional setting; null when it is missing or empty.</summary>
    public static string? TryGet(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        return Values.Value.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    private static Dictionary<string, string> Load()
    {
        var values = new Dictionary<string, string>();

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, ".env");
            if (!File.Exists(path))
                continue;

            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                var separator = trimmed.IndexOf('=');
                if (trimmed.StartsWith('#') || separator <= 0)
                    continue;

                values[trimmed[..separator].Trim()] = trimmed[(separator + 1)..].Trim().Trim('"');
            }

            break;
        }

        return values;
    }
}
