using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using RestSharp;

namespace WhereIsMyAi.RestSharpBase.Core.Helpers;

/// <summary>The only place that touches the JSON serializer.</summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>The JSON text sent for a request body; a JsonObject is written as built (nulls kept, omitted properties absent).</summary>
    public static string ToJson<T>(T body) => JsonSerializer.Serialize(body, Options);

    /// <summary>JSON text as a T; throws when it is not one.</summary>
    public static T FromJson<T>(string json) where T : class =>
        JsonSerializer.Deserialize<T>(json, Options) ?? throw new InvalidOperationException($"Expected a {typeof(T).Name}, got: '{json}'.");

    /// <summary>response.ContentAs&lt;MyDto&gt;() - the body as a MyDto; throws with the status and body when it is not one.</summary>
    public static T ContentAs<T>(this RestResponse response) where T : class
    {
        var error = $"Expected a {typeof(T).Name} in the response body (status {(int)response.StatusCode}), got: '{response.Content}'.";
        try
        {
            // Empty body, HTML or an array -> JsonException; the JSON literal null -> null.
            return JsonSerializer.Deserialize<T>(response.Content ?? "", Options) ?? throw new InvalidOperationException(error);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(error, ex);
        }
    }

    /// <summary>The raw body as a JsonObject, to check which properties are present and their JSON types.</summary>
    public static JsonObject ContentAsJsonObject(this RestResponse response) => response.ContentAs<JsonObject>();
}
