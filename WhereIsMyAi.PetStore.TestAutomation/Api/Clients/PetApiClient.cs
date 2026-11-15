using System.Text.Json.Nodes;
using WhereIsMyAi.RestSharpBase.Core.Http;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.Api.Clients;

/// <summary>Business methods for the /pet endpoints. Reqnroll DI creates one instance per scenario.</summary>
public class PetApiClient() : RestApiClient(PetStoreConfig.BaseUrl)
{
    // A JsonObject, not a PetDto: tests decide which properties are sent, null or left out.
    public RestResponse CreatePet(JsonObject pet) => Post("/pet", pet);

    // String IDs, so tests can send invalid ones ("abc").
    public RestResponse GetPetById(string petId) => Get($"/pet/{petId}");

    public RestResponse DeletePetById(string petId) => Delete($"/pet/{petId}");
}
