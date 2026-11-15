using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace WhereIsMyAi.RestSharpBase.Core.Tms;

/// <summary>
/// Local bug repository: a folder with one Markdown file per bug, named after the bug id (BUG-001.md).
/// The file holds "Key: value" lines (a Markdown list item "- Key: value" works too); the repository reads
/// Id, Status and Title, everything else is free text for people:
/// <code>
/// # BUG-001: Saving a record twice creates a duplicate
/// - Id: BUG-001
/// - Status: Open
/// - Title: Saving a record twice creates a duplicate
/// </code>
/// </summary>
public class BugRepository(string path)
{
    private static readonly Regex Field = new(@"^\s*[-*]?\s*(?<key>Id|Status|Title)\s*:\s*(?<value>.+?)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline);

    /// <summary>Path of the bug repository folder.</summary>
    public string Path { get; } = path;

    public bool IsAvailable => Directory.Exists(Path);

    /// <summary>The bug with this id, or null when the repository has no file for it.</summary>
    public Bug? Find(string id)
    {
        var file = System.IO.Path.Combine(Path, id + ".md"); // System.IO: Path is the property here
        if (!File.Exists(file))
            return null;

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Field.Matches(File.ReadAllText(file)))
            fields.TryAdd(match.Groups["key"].Value, match.Groups["value"].Value);

        return new Bug(
            fields.GetValueOrDefault("Id", id),
            fields.GetValueOrDefault("Status", ""),
            fields.GetValueOrDefault("Title", ""));
    }
}
