using System;

namespace WhereIsMyAi.RestSharpBase.Core.Extensions;

/// <summary>Generic string extensions.</summary>
public static class StringExtensions
{
    /// <summary>"12345" -> 12345; anything else fails with a readable message ("Id 'abc' is not a whole number").</summary>
    public static long ToLong(this string value, string name = "Value") =>
        long.TryParse(value, out var number)
            ? number
            : throw new FormatException($"{name} '{value}' is not a whole number.");
}
