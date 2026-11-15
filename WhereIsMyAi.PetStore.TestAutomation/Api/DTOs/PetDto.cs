using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;

/// <summary>Request and response model of a pet. Every field is nullable: null = not sent / not returned.</summary>
public class PetDto
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("category")]
    public CategoryDto? Category { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("photoUrls")]
    public List<string>? PhotoUrls { get; set; }

    [JsonPropertyName("tags")]
    public List<TagDto>? Tags { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
