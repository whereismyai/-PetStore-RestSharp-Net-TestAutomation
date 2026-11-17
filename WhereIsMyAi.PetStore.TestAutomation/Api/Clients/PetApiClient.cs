using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;
using WhereIsMyAi.RestSharpBase.Core.Http;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.Api.Clients;

/// <summary>Business methods for the /pet endpoints. Reqnroll DI creates one instance per scenario.</summary>
public class PetApiClient() : RestApiClient(PetStoreConfig.BaseUrl)
{
    public RestResponse CreatePet(PetDto pet) => Post("/pet", pet);

    // Sends the body exactly as written (broken JSON, wrong JSON types, XML).
    public RestResponse CreatePet(string body, string contentType) =>
        Execute(new RestRequest("/pet", Method.Post).AddStringBody(body, contentType));

    public RestResponse CreatePet(string body, string contentType, string accept) =>
        Execute(new RestRequest("/pet", Method.Post).AddStringBody(body, contentType).AddHeader(KnownHeaders.Accept, accept));

    // String Ids, so tests can send invalid ones ("abc").
    public RestResponse GetPetById(string petId) => Get($"/pet/{petId}");

    // Asks for one media type; without it RestSharp's default Accept lists JSON first, then XML.
    public RestResponse GetPetById(string petId, string accept) =>
        Execute(new RestRequest($"/pet/{petId}").AddHeader(KnownHeaders.Accept, accept));

    public RestResponse DeletePetById(string petId) => Delete($"/pet/{petId}");

    public RestResponse DeletePetById(string petId, string apiKey) =>
        Execute(new RestRequest($"/pet/{petId}", Method.Delete).AddHeader("api_key", apiKey));
}
