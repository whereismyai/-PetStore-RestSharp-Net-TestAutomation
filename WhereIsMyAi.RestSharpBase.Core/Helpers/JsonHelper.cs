using System;
using System.Text.Json;
using RestSharp;

namespace WhereIsMyAi.RestSharpBase.Core.Helpers;

/// <summary>The only place that touches the JSON serializer.</summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

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
}
