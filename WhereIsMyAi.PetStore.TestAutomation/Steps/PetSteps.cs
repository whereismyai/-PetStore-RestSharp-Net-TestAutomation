using System;
using System.Linq;
using FluentAssertions;
using WhereIsMyAi.PetStore.TestAutomation.Api.Clients;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;
using WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;
using WhereIsMyAi.RestSharpBase.Core.Extensions;
using WhereIsMyAi.RestSharpBase.Core.Helpers;
using Reqnroll;

namespace WhereIsMyAi.PetStore.TestAutomation.Steps;

/// <summary>Pet steps. A pet is addressed by ID, by name (unique per scenario) or as the last created pet.</summary>
[Binding]
public class PetSteps(PetApiClient petApi, CreatedPets createdPets, LastResponse lastResponse, RunIdInt runIdInt)
    : BaseStep(runIdInt)
{
    #region Pet creation - POST /pet

    // Repeated PhotoUrl / Tag rows build lists; Category and Tag take 'id:name' or 'name'; 'null' leaves a field out.
    [When(@"^a pet is created with the following values via POST /pet$")]
    public void WhenAPetIsCreatedWithTheFollowingValuesViaPostPet(Table table)
    {
        var response = petApi.CreatePet(BuildPet(table));
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
                case "photourl": pet.PhotoUrls.Add(value); break;
                case "tag": pet.Tags.Add(ParseTag(value)); break;
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

    // For pets the scenario did not create; the ID is sent as written, so 'abc' works too.
    [When(@"^the pet with ID '(.*)' is retrieved via GET /pet/\{id\}$")]
    public void WhenThePetWithIdIsRetrievedViaGetPetId(string petId) => GetPet(ReplaceVariables(petId));

    [When(@"^the created pet named '(.*)' is retrieved via GET /pet/\{id\}$")]
    public void WhenTheCreatedPetNamedIsRetrievedViaGetPetId(string name) =>
        GetPet(createdPets.GetPetIdByName(ReplaceVariables(name)).ToString());

    [When(@"^the last created pet is retrieved via GET /pet/\{id\}$")]
    public void WhenTheLastCreatedPetIsRetrievedViaGetPetId() => GetPet(createdPets.GetLastPetId().ToString());

    private void GetPet(string petId) => lastResponse.Value = petApi.GetPetById(petId);

    #endregion

    #region Pet deletion - DELETE /pet/{id}

    [When(@"^the pet with ID '(.*)' is deleted via DELETE /pet/\{id\}$")]
    public void WhenThePetWithIdIsDeletedViaDeletePetId(string petId) => DeletePet(ReplaceVariables(petId));

    [When(@"^the created pet named '(.*)' is deleted via DELETE /pet/\{id\}$")]
    public void WhenTheCreatedPetNamedIsDeletedViaDeletePetId(string name) =>
        DeletePet(createdPets.GetPetIdByName(ReplaceVariables(name)).ToString());

    [When(@"^the last created pet is deleted via DELETE /pet/\{id\}$")]
    public void WhenTheLastCreatedPetIsDeletedViaDeletePetId() => DeletePet(createdPets.GetLastPetId().ToString());

    private void DeletePet(string petId) => lastResponse.Value = petApi.DeletePetById(petId);

    #endregion

    #region Pet response checks

    // Known Id: the table is the whole expected pet, in the same format as on creation; every field must be equal.
    [Then(@"^the response should have '(\d+)' status code and the returned pet should be exactly$")]
    public void ThenTheResponseShouldHaveStatusCodeAndTheReturnedPetShouldBeExactly(int statusCode, Table table) =>
        AssertMatches(statusCode, BuildPet(table));

    // Generated Id: the whole pet must equal the created one, as the POST returned it.
    [Then(@"^the response should have '(\d+)' status code and the returned pet should match the created pet named '(.*)'$")]
    public void ThenTheResponseShouldHaveStatusCodeAndTheReturnedPetShouldMatchTheCreatedPetNamed(int statusCode, string name) =>
        AssertMatches(statusCode, createdPets.GetPetByName(ReplaceVariables(name)));

    [Then(@"^the created pets should have different IDs$")]
    public void ThenTheCreatedPetsShouldHaveDifferentIds() =>
        createdPets.Value.Select(pet => pet.Id).Should().OnlyHaveUniqueItems();

    private void AssertMatches(int statusCode, PetDto expected) =>
        ReadPet(statusCode).Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());

    private PetDto ReadPet(int statusCode)
    {
        var response = lastResponse.Value;
        ((int)response.StatusCode).Should().Be(statusCode, "response body: {0}", response.Content);
        return response.ContentAs<PetDto>();
    }

    #endregion
}
