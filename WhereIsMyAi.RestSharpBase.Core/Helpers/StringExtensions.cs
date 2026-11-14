using System;
using System.Collections.Generic;
using System.Linq;

namespace WhereIsMyAi.RestSharpBase.Core.Helpers;

public static class StringExtensions
{
    /// <summary>
    /// Separator for list values in test data tables. A symbol that is not on the keyboard and will
    /// not appear in real test data (unlike a comma, which can be part of a value).
    /// </summary>
    public const char SpecSymbol = '↕';

    /// <summary>"a ↕ b↕c" -> ["a", "b", "c"]; empty input -> empty list.</summary>
    public static List<string> SplitBySpecSymbol(this string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? new List<string>()
            : value.Split(SpecSymbol, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
}
