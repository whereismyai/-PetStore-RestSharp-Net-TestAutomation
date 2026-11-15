namespace WhereIsMyAi.PetStore.TestAutomation.Api.Data;

/// <summary>PetStore endpoint URLs.</summary>
public static class Endpoints
{
    public const string Pet = "/pet";

    // String IDs, so tests can send invalid ones ("abc").
    public static string PetById(string petId) => $"/pet/{petId}";
}
