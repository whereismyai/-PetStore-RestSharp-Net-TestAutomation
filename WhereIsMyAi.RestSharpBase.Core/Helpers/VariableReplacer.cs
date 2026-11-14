using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WhereIsMyAi.RestSharpBase.Core.Helpers;

/// <summary>
/// Replaces variable placeholders in a string: "{name}" or "{name:argument}". The
/// caller supplies the variables as name -> resolver (the resolver gets the argument, or null without one).
/// Placeholders with an unknown name are left as they are, so other "{...}" text is not touched.
/// </summary>
public static class VariableReplacer
{
    private static readonly Regex Placeholder = new(@"\{(?<name>[\w-]+)(?::(?<argument>[^{}]*))?\}");

    /// <summary>"{run_id_int}12345" with run_id_int = 4821 -> "482112345".</summary>
    public static string ReplaceVariables(this string value, IReadOnlyDictionary<string, Func<string?, string>> variables) =>
        Placeholder.Replace(value, match =>
            variables.TryGetValue(match.Groups["name"].Value, out var resolve)
                ? resolve(match.Groups["argument"].Success ? match.Groups["argument"].Value : null)
                : match.Value);
}
