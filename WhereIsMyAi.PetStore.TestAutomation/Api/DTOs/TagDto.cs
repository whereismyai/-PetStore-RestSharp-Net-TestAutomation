using System.Text.Json.Serialization;

namespace WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;

public class TagDto
{
    [JsonPropertyName("id")] public long? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
}