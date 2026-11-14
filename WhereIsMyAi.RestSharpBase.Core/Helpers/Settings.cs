using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace WhereIsMyAi.RestSharpBase.Core.Helpers;

/// <summary>
/// Test settings via Microsoft.Extensions.Configuration, each source overriding the one before:
/// appsettings.json (committed defaults), appsettings.local.json (optional, gitignored, for local changes),
/// then environment variables (for CI). The folder holding appsettings.json is found by searching upward
/// from the build output folder, so a run from bin/Debug/... uses the repository's file; relative paths in
/// settings are resolved against that folder.
/// </summary>
public static class Settings
{
    private const string FileName = "appsettings.json";
    private const string LocalFileName = "appsettings.local.json";

    private static readonly Lazy<(IConfiguration Configuration, string Folder)> Loaded = new(Load);

    /// <summary>A required setting; throws when it is missing.</summary>
    public static string Get(string key) =>
        TryGet(key) ?? throw new InvalidOperationException(
            $"Setting '{key}' not found. Add it to {FileName} or set it as an environment variable.");

    /// <summary>An optional setting; null when it is missing or empty.</summary>
    public static string? TryGet(string key)
    {
        var value = Loaded.Value.Configuration[key];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>An optional path setting as a full path, relative to the settings folder; null when it is missing.</summary>
    public static string? TryGetPath(string key)
    {
        var value = TryGet(key);
        return value == null ? null : Path.GetFullPath(value, Loaded.Value.Folder);
    }

    private static (IConfiguration Configuration, string Folder) Load()
    {
        var folder = FindSettingsFolder() ?? throw new InvalidOperationException(
            $"{FileName} not found in {AppContext.BaseDirectory} or any folder above it.");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(folder)
            .AddJsonFile(FileName, optional: false)
            .AddJsonFile(LocalFileName, optional: true)
            .AddEnvironmentVariables()
            .Build();

        return (configuration, folder);
    }

    private static string? FindSettingsFolder()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, FileName)))
                return dir.FullName;
        }

        return null;
    }
}
