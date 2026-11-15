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
    private static readonly Lazy<EnvFile> File = new(() => Load(AppContext.BaseDirectory));

    /// <summary>The settings of a .env file and the path of its folder (null when no .env file was found).</summary>
    internal sealed record EnvFile(Dictionary<string, string> Values, string? FolderPath);

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

    /// <summary>
    /// An optional path setting as a full path; null when it is missing. A relative path is resolved against
    /// the folder of the .env file (the current directory when there is no .env file).
    /// </summary>
    public static string? GetOptionalPath(string key) => GetOptionalPath(key, File.Value);

    internal static string? GetOptionalPath(string key, EnvFile file)
    {
        var value = GetOptional(key, file);
        return value == null ? null : Path.GetFullPath(value, file.FolderPath ?? Directory.GetCurrentDirectory());
    }

    /// <summary>The first .env file found in startFolder or above it.</summary>
    internal static EnvFile Load(string startFolder)
    {
        var values = new Dictionary<string, string>();

        for (var dir = new DirectoryInfo(startFolder); dir != null; dir = dir.Parent)
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
