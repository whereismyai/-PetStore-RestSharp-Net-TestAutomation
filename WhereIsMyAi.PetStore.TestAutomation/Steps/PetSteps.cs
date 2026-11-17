using System;
using System.Linq;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using WhereIsMyAi.PetStore.TestAutomation.Api.Clients;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;
using WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;
using WhereIsMyAi.RestSharpBase.Core.Extensions;
using WhereIsMyAi.RestSharpBase.Core.Helpers;
using WhereIsMyAi.RestSharpBase.Core.Logger;
using Reqnroll;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.Steps;

/// <summary>Pet steps. A pet is addressed by Id, by name (unique per scenario) or as the last created pet.</summary>
[Binding]
public class PetSteps(PetApiClient petApi, CreatedPets createdPets, LastResponse lastResponse, RunIdInt runIdInt)
    : BaseStep(runIdInt)
{
    private static ILogger Log => InternalLogger.Logger;

    #region Preconditions

    // Table 'Id', one row per valid Id the scenario uses, before its first request: GET must be 404; a 200 (leftover pet) is deleted and rechecked.
    [Given(@"^the pets with the following Ids do not exist$")]
    public void GivenThePetsWithTheFollowingIdsDoNotExist(Table table)
    {
        foreach (var row in table.Rows)
        {
            var id = ReplaceVariables(row["Id"]);
            createdPets.SentIds.Add(id.ToLong("Pet Id")); // cleanup deletes it even when a raw-body POST stored it
            EnsurePetDoesNotExist(id);
        }
    }

    private void EnsurePetDoesNotExist(string id)
    {
        var found = petApi.GetPetById(id);
        if (found.StatusCode == HttpStatusCode.NotFound)
            return;

        found.StatusCode.Should().Be(HttpStatusCode.OK, "precondition GET /pet/{0} returned: {1}", id, found.Content);
        Log.LogWarning("Pet {PetId} already exists before the scenario (earlier run or another user) - deleting it", id);
        petApi.DeletePetById(id).StatusCode.Should().Be(HttpStatusCode.OK, "precondition DELETE /pet/{0} must remove the pet", id);
        petApi.GetPetById(id).StatusCode.Should().Be(HttpStatusCode.NotFound, "pet {0} must be gone before the scenario starts", id);
    }

    #endregion

    #region Pet creation - POST /pet

    // Repeated PhotoUrl / Tag rows build lists; Category and Tag take 'id:name' or 'name'; 'null' leaves a field out (not sent).
    [When(@"^a pet is created with the following values via POST /pet$")]
    public void WhenAPetIsCreatedWithTheFollowingValuesViaPostPet(Table table)
    {
        var pet = BuildPet(table);
        if (pet.Id != null) // recorded before sending: cleanup deletes it even if the reply has a wrong Id or none
            createdPets.SentIds.Add(pet.Id.Value);

        RecordCreation(petApi.CreatePet(pet));
    }

    // The doc string is sent exactly as written: broken JSON, wrong JSON types or XML. Use for what a table cannot express.
    [When(@"^a pet is created with the following '(.*)' body via POST /pet$")]
    public void WhenAPetIsCreatedWithTheFollowingBodyViaPostPet(string contentType, string body) =>
        RecordCreation(petApi.CreatePet(ReplaceVariables(body), ReplaceVariables(contentType)));

    [When(@"^a pet is created with an empty '(.*)' body via POST /pet$")]
    public void WhenAPetIsCreatedWithAnEmptyBodyViaPostPet(string contentType) =>
        RecordCreation(petApi.CreatePet("", ReplaceVariables(contentType)));

    private void RecordCreation(RestResponse response)
    {
        lastResponse.Value = response;
        if (response.IsSuccessful)
            createdPets.Value.Add(response.ContentAs<PetDto>());
    }

    private PetDto BuildPet(Table table)
    {
        var pet = new PetDto();

        foreach (var row in table.Rows)
        {
            var value = ReplaceVariables(row["Value"]);
            if (value == "null")
                continue;

            switch (row["Field"].ToLowerInvariant())
            {
                case "id": pet.Id = value.ToLong("Pet Id"); break;
                case "name": pet.Name = value; break;
                case "status": pet.Status = value; break;
                case "category": pet.Category = ParseCategory(value); break;
                case "photourl": (pet.PhotoUrls ??= new()).Add(value); break;
                case "tag": (pet.Tags ??= new()).Add(ParseTag(value)); break;
                default: throw new NotSupportedException(
                    $"Field '{row["Field"]}' is not supported. Use Id, Name, Status, Category, PhotoUrl or Tag.");
            }
        }

        return pet;
    }

    private static CategoryDto ParseCategory(string value)
    {
        var (id, name) = SplitIdAndName(value);
        return new CategoryDto { Id = id, Name = name };
    }

    private static TagDto ParseTag(string value)
    {
        var (id, name) = SplitIdAndName(value);
        return new TagDto { Id = id, Name = name };
    }

    // "1:friendly" -> (1, "friendly"); "friendly" -> (null, "friendly")
    private static (long? Id, string Name) SplitIdAndName(string value)
    {
        var parts = value.Split(':', 2);
        return parts.Length == 2 && long.TryParse(parts[0], out var id) ? (id, parts[1].Trim()) : (null, value.Trim());
    }

    #endregion

    #region Pet retrieval - GET /pet/{id}

    // For pets the scenario did not create; the Id is sent as written, so 'abc' works too.
    [When(@"^the pet with Id '(.*)' is retrieved via GET /pet/\{id\}$")]
    public void WhenThePetWithIdIsRetrievedViaGetPetId(string petId) => GetPet(ReplaceVariables(petId));

    [When(@"^the created pet named '(.*)' is retrieved via GET /pet/\{id\}$")]
    public void WhenTheCreatedPetNamedIsRetrievedViaGetPetId(string name) =>
        GetPet(createdPets.GetPetIdByName(ReplaceVariables(name)).ToString());

    [When(@"^the last created pet is retrieved via GET /pet/\{id\}$")]
    public void WhenTheLastCreatedPetIsRetrievedViaGetPetId() => GetPet(createdPets.GetLastPetId().ToString());

    // Content negotiation: sends only this media type in Accept; the other GET steps keep the client's default.
    [When(@"^the last created pet is retrieved with Accept '(.*)' via GET /pet/\{id\}$")]
    public void WhenTheLastCreatedPetIsRetrievedWithAcceptViaGetPetId(string accept) =>
        lastResponse.Value = petApi.GetPetById(createdPets.GetLastPetId().ToString(), ReplaceVariables(accept));

    private void GetPet(string petId) => lastResponse.Value = petApi.GetPetById(petId);

    #endregion

    #region Pet deletion - DELETE /pet/{id}

    [When(@"^the pet with Id '(.*)' is deleted via DELETE /pet/\{id\}$")]
    public void WhenThePetWithIdIsDeletedViaDeletePetId(string petId) => DeletePet(ReplaceVariables(petId));

    [When(@"^the created pet named '(.*)' is deleted via DELETE /pet/\{id\}$")]
    public void WhenTheCreatedPetNamedIsDeletedViaDeletePetId(string name) =>
        DeletePet(createdPets.GetPetIdByName(ReplaceVariables(name)).ToString());

    [When(@"^the last created pet is deleted via DELETE /pet/\{id\}$")]
    public void WhenTheLastCreatedPetIsDeletedViaDeletePetId() => DeletePet(createdPets.GetLastPetId().ToString());

    // The spec defines an optional api_key header for DELETE; the other DELETE steps send none.
    [When(@"^the last created pet is deleted with api_key '(.*)' via DELETE /pet/\{id\}$")]
    public void WhenTheLastCreatedPetIsDeletedWithApiKeyViaDeletePetId(string apiKey) =>
        lastResponse.Value = petApi.DeletePetById(createdPets.GetLastPetId().ToString(), ReplaceVariables(apiKey));

    private void DeletePet(string petId) => lastResponse.Value = petApi.DeletePetById(petId);

    #endregion

    #region Pet response checks

    // Known Id: the table is the whole expected pet, in the same format as on creation; every field must be equal.
    [Then(@"^the response should have '(\d+)' status code and the returned pet should be exactly$")]
    public void ThenTheResponseShouldHaveStatusCodeAndTheReturnedPetShouldBeExactly(int statusCode, Table table) =>
        AssertMatches(statusCode, ToExpectedPet(table));

    // Generated Id, POST: a positive Id (observed), and every other field must equal the table (never compare with an earlier response).
    [Then(@"^the response should have '(\d+)' status code and the returned pet should be exactly, with a generated Id$")]
    public void ThenTheResponseShouldHaveStatusCodeAndTheReturnedPetShouldBeExactlyWithAGeneratedId(int statusCode, Table table)
    {
        var pet = ReadPet(statusCode);
        pet.Id.Should().BePositive("a generated Id is positive: {0}", lastResponse.Value.Content);
        pet.Should().BeEquivalentTo(ToExpectedPet(table), options => options.WithStrictOrdering().Excluding(p => p.Id),
            "response body: {0}", lastResponse.Value.Content);
    }

    // Generated Id, later reads: the table plus the Id the POST returned for that pet.
    [Then(@"^the response should have '(\d+)' status code and the returned pet should be exactly, with the Id of the created pet named '(.*)'$")]
    public void ThenTheResponseShouldHaveStatusCodeAndTheReturnedPetShouldBeExactlyWithTheIdOfTheCreatedPetNamed(
        int statusCode, string name, Table table)
    {
        var expected = ToExpectedPet(table);
        expected.Id = createdPets.GetPetIdByName(ReplaceVariables(name));
        AssertMatches(statusCode, expected);
    }

    [Then(@"^the created pets should have different Ids$")]
    public void ThenTheCreatedPetsShouldHaveDifferentIds() =>
        createdPets.Value.Select(pet => pet.Id).Should().OnlyHaveUniqueItems();

    // Observed server default, not a contract rule: photoUrls and tags come back as [] when none were sent.
    private PetDto ToExpectedPet(Table table)
    {
        var pet = BuildPet(table);
        pet.PhotoUrls ??= new();
        pet.Tags ??= new();
        return pet;
    }

    private void AssertMatches(int statusCode, PetDto expected) =>
        ReadPet(statusCode).Should().BeEquivalentTo(expected, options => options.WithStrictOrdering(),
            "response body: {0}", lastResponse.Value.Content);

    // Wrong JSON types fail in ContentAs; required fields (contract) and id (the pet is read back by it) fail here by name, in exact case.
    private PetDto ReadPet(int statusCode)
    {
        var response = lastResponse.Value;
        ((int)response.StatusCode).Should().Be(statusCode, "response body: {0}", response.Content);

        var pet = response.ContentAs<PetDto>();
        pet.Id.Should().NotBeNull("'id' must be in the response: {0}", response.Content);
        pet.Name.Should().NotBeNull("'name' is required: {0}", response.Content);
        pet.PhotoUrls.Should().NotBeNull("'photoUrls' is required: {0}", response.Content);
        return pet;
    }

    #endregion
}
