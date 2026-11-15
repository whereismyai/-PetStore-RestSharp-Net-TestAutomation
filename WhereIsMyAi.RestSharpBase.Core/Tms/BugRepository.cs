using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WhereIsMyAi.RestSharpBase.Core.Tms;

/// <summary>Local bug repository: one &lt;id&gt;.md per bug with "Id:", "Status:" and "Title:" lines (Markdown list items work too).</summary>
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
            ParseStatus(fields.GetValueOrDefault("Status")),
            fields.GetValueOrDefault("Title", ""));
    }

    // Only the exact names of BugStatus, any case: "Opne" or "1" is not a status.
    private static BugStatus? ParseStatus(string? value) =>
        value != null && Enum.GetNames<BugStatus>().Contains(value, StringComparer.OrdinalIgnoreCase)
            ? Enum.Parse<BugStatus>(value, ignoreCase: true)
            : null;
}
