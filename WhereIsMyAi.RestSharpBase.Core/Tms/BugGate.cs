using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.RestSharpBase.Core.Logger;

namespace WhereIsMyAi.RestSharpBase.Core.Tms;

/// <summary>Skip decision for tests tagged bug:&lt;id&gt;: skip while the bug is Open; when it cannot tell, warn and run.</summary>
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

            if (bug.Status == null)
            {
                Log.LogWarning("Bug {BugId} has no valid Status (one of {Statuses}) - test runs",
                    bug.Id, string.Join(", ", Enum.GetNames<BugStatus>()));
                continue;
            }

            if (bug.IsOpen)
                return $"Blocked by open bug {bug.Id}: {bug.Title}";

            Log.LogInformation("Bug {BugId} is {Status} - linked test runs", bug.Id, bug.Status);
        }

        return null;
    }
}
