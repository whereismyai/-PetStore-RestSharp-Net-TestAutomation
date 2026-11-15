using System;
using System.Collections.Generic;
using System.IO;

namespace WhereIsMyAi.RestSharpBase.Core.Helpers;

/// <summary>KEY=VALUE settings: a real environment variable wins, else the first .env file found upward from the build output.</summary>
public static class EnvFileHelper
{
    private static readonly Lazy<EnvFile> File = new(() => Load(AppContext.BaseDirectory));

    /// <summary>The settings of a .env file and the path of the folder that holds it (null when no .env file was found).</summary>
    internal sealed record EnvFile(Dictionary<string, string> Values, string? Path);

    /// <summary>A required setting; throws when it is missing.</summary>
    public static string Get(string key) =>
        GetOptional(key) ?? throw new InvalidOperationException(
            $"Setting '{key}' not found. Add it to a .env file (see .env.example) or set it as an environment variable.");

    /// <summary>An optional setting; null when it is missing or empty.</summary>
    public static string? GetOptional(string key) => GetOptional(key, File.Value);

    internal static string? GetOptional(string key, EnvFile file)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        return file.Values.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    /// <summary>An optional path setting as a full path, relative ones resolved against the .env folder; null when missing.</summary>
    public static string? GetOptionalPath(string key) => GetOptionalPath(key, File.Value);

    internal static string? GetOptionalPath(string key, EnvFile file)
    {
        var value = GetOptional(key, file);
        return value == null ? null : Path.GetFullPath(value, file.Path ?? Directory.GetCurrentDirectory());
    }

    /// <summary>The first .env file found in startPath or above it.</summary>
    internal static EnvFile Load(string startPath)
    {
        var values = new Dictionary<string, string>();

        for (var dir = new DirectoryInfo(startPath); dir != null; dir = dir.Parent)
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

            return new EnvFile(values, dir.FullName);
        }

        return new EnvFile(values, null);
    }
}
