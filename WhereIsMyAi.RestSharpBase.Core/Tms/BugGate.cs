using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.RestSharpBase.Core.Logger;

namespace WhereIsMyAi.RestSharpBase.Core.Tms;

/// <summary>
/// Decides whether a test linked to a bug must be skipped. A test is linked by a tag "bug:&lt;id&gt;"
/// (e.g. bug:BUG-001); it is skipped while that bug is Open in the <see cref="BugRepository"/>.
/// Never skips when it cannot tell: no repository configured, path missing, or unknown bug id - those
/// cases only log a warning and the test runs. Framework-agnostic: the caller performs the actual skip.
/// </summary>
/// <param name="bugRepositoryPath">Path of the bug repository folder; null when it is not configured.</param>
public class BugGate(string? bugRepositoryPath)
{
    public const string TagPrefix = "bug:";

    private readonly BugRepository? _repository = bugRepositoryPath == null ? null : new BugRepository(bugRepositoryPath);

    private static ILogger Log => InternalLogger.Logger;

    /// <summary>The reason to skip the test (its open bug), or null when it must run.</summary>
    public string? GetSkipReason(IEnumerable<string> tags)
    {
        var bugIds = tags
            .Where(tag => tag.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(tag => tag[TagPrefix.Length..])
            .ToList();
        if (bugIds.Count == 0)
            return null;

        if (_repository == null || !_repository.IsAvailable)
        {
            Log.LogWarning("Bug repository path is unavailable ({Path}) - test linked to {BugIds} runs instead of being skipped",
                _repository?.Path ?? "not configured", string.Join(", ", bugIds));
            return null;
        }

        foreach (var bugId in bugIds)
        {
            var bug = _repository.Find(bugId);
            if (bug == null)
            {
                Log.LogWarning("Bug {BugId} not found in bug repository {Path} - test runs", bugId, _repository.Path);
                continue;
            }

            if (bug.IsOpen)
                return $"Blocked by open bug {bug.Id}: {bug.Title}";

            Log.LogInformation("Bug {BugId} is {Status} - linked test runs", bug.Id, bug.Status);
        }

        return null;
    }
}
