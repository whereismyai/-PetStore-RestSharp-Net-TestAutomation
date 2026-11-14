using System.Text.Json.Serialization;

namespace WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;

/// <summary>
/// The API's generic reply (the spec's <c>ApiResponse</c> model), returned for errors and for DELETE,
/// e.g. {"code":1,"type":"error","message":"Pet not found"}.
/// </summary>
public class ApiResponseDto
{
    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
