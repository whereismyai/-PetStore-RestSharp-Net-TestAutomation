using System.Text.Json;
using RestSharp;

namespace WhereIsMyAi.RestSharpBase.Core.Helpers;

/// <summary>The only place that touches the JSON serializer.</summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>JSON text -> object; null for empty input.</summary>
    private static T? Deserialize<T>(string? json) =>
        string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, Options);

    /// <summary>response.Content&lt;MyDto&gt;() - the response body as an object; null for an empty body.</summary>
    public static T? Content<T>(this RestResponse response) => Deserialize<T>(response.Content);
}
