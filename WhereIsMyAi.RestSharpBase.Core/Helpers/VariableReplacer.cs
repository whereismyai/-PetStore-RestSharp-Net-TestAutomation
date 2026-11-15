using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WhereIsMyAi.RestSharpBase.Core.Helpers;

/// <summary>Replaces "{name}" placeholders with values from a caller-supplied map; unknown names are left as they are.</summary>
public static class VariableReplacer
{
    private static readonly Regex Placeholder = new(@"\{(?<name>[\w-]+)\}");

    /// <summary>"{run_id_int}12345" with run_id_int = 4821 -> "482112345".</summary>
    public static string ReplaceVariables(this string value, IReadOnlyDictionary<string, Func<string>> variables) =>
        Placeholder.Replace(value, match =>
            variables.TryGetValue(match.Groups["name"].Value, out var resolve) ? resolve() : match.Value);
}
