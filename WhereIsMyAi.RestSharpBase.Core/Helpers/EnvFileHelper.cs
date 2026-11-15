using System;
using System.Collections.Generic;
using System.IO;

namespace WhereIsMyAi.RestSharpBase.Core.Helpers;

/// <summary>
/// Reads KEY=VALUE settings from a .env file, found by searching upward from the build output
/// folder (so it works from bin/Debug/... and from the repo root). A real environment variable
/// with the same name wins, which lets CI override the file without having one.
/// </summary>
public static class EnvFileHelper
{
    private static readonly Lazy<(Dictionary<string, string> Values, string? Folder)> File = new(Load);

    /// <summary>A required setting; throws when it is missing.</summary>
    public static string Get(string key) =>
        GetOptional(key) ?? throw new InvalidOperationException(
            $"Setting '{key}' not found. Add it to a .env file (see .env.example) or set it as an environment variable.");

    /// <summary>An optional setting; null when it is missing or empty.</summary>
    public static string? GetOptional(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        return File.Value.Values.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    /// <summary>
    /// An optional path setting as a full path; null when it is missing. A relative path is resolved against
    /// the folder of the .env file (the current directory when there is no .env file).
    /// </summary>
    public static string? GetOptionalPath(string key)
    {
        var value = GetOptional(key);
        return value == null ? null : Path.GetFullPath(value, File.Value.Folder ?? Directory.GetCurrentDirectory());
    }

    private static (Dictionary<string, string> Values, string? Folder) Load()
    {
        var values = new Dictionary<string, string>();

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, ".env");
            if (!System.IO.File.Exists(path))
                continue;

            foreach (var line in System.IO.File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                var separator = trimmed.IndexOf('=');
                if (trimmed.StartsWith('#') || separator <= 0)
                    continue;

                values[trimmed[..separator].Trim()] = trimmed[(separator + 1)..].Trim().Trim('"');
            }

            return (values, dir.FullName);
        }

        return (values, null);
    }
}
