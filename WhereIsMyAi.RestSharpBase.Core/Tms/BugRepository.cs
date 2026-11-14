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
/// # BUG-001: POST /pet with an existing Id overwrites the pet
/// - Id: BUG-001
/// - Status: Open
/// - Title: POST /pet with an existing Id overwrites the pet
/// </code>
/// </summary>
public class BugRepository
{
    private static readonly Regex Field = new(@"^\s*[-*]?\s*(?<key>Id|Status|Title)\s*:\s*(?<value>.+?)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public BugRepository(string folder)
    {
        Folder = folder;
    }

    public string Folder { get; }

    public bool IsAvailable => Directory.Exists(Folder);

    /// <summary>The bug with this id, or null when the repository has no file for it.</summary>
    public Bug? Find(string id)
    {
        var path = Path.Combine(Folder, id + ".md");
        if (!File.Exists(path))
            return null;

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Field.Matches(File.ReadAllText(path)))
            fields.TryAdd(match.Groups["key"].Value, match.Groups["value"].Value);

        return new Bug(
            fields.GetValueOrDefault("Id", id),
            fields.GetValueOrDefault("Status", ""),
            fields.GetValueOrDefault("Title", ""));
    }
}
