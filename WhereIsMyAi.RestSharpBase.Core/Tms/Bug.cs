using System;

namespace WhereIsMyAi.RestSharpBase.Core.Tms;

/// <summary>A bug raised in the local bug repository (one Markdown file per bug).</summary>
public record Bug(string Id, string Status, string Title)
{
    public const string OpenStatus = "Open";

    /// <summary>Open bugs block the tests linked to them; any other status (Fixed, Closed, ...) does not.</summary>
    public bool IsOpen => Status.Equals(OpenStatus, StringComparison.OrdinalIgnoreCase);
}
