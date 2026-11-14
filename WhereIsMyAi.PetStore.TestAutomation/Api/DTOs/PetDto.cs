using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;

public class PetDto
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("category")]
    public Category? Category { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("photoUrls")]
    public List<string> PhotoUrls { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<Tag> Tags { get; set; } = new();

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class Category
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class Tag
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
