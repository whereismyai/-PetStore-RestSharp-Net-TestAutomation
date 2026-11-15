using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using RestSharp;

namespace WhereIsMyAi.RestSharpBase.Core.Helpers;

/// <summary>The only place that touches the JSON serializer.</summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // an unset (null) property is not sent
    };

    /// <summary>A request body as JSON, without its null properties.</summary>
    public static string ToJson<T>(T body) => JsonSerializer.Serialize(body, Options);

    /// <summary>response.ContentAs&lt;MyDto&gt;() - the body as a MyDto; throws with the status and body when it is not one
    /// (empty, HTML, an array, the literal null, or a value of the wrong JSON type).</summary>
    public static T ContentAs<T>(this RestResponse response) where T : class
    {
        var error = $"Expected a {typeof(T).Name} in the response body (status {(int)response.StatusCode}), got: '{response.Content}'.";
        try
        {
            return JsonSerializer.Deserialize<T>(response.Content ?? "", Options) ?? throw new InvalidOperationException(error);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(error, ex);
        }
    }
}
