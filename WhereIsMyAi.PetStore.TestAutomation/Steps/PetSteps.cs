using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using WhereIsMyAi.PetStore.TestAutomation.Api.Clients;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;
using WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;
using WhereIsMyAi.RestSharpBase.Core.Helpers;
using WhereIsMyAi.PetStore.TestAutomation.Utils;
using Reqnroll;

namespace WhereIsMyAi.PetStore.TestAutomation.Steps;

/// <summary>Pet steps. A pet is addressed by ID, by name (unique per scenario) or as the last created pet.</summary>
[Binding]
public class PetSteps : BaseStep
{
    private readonly PetApiClient _petApi;
    private readonly CreatedPets _createdPets;
    private readonly LastResponse _lastResponse;

    public PetSteps(PetApiClient petApi, CreatedPets createdPets, LastResponse lastResponse, VariableRepository variables)
        : base(variables)
    {
        _petApi = petApi;
        _createdPets = createdPets;
        _lastResponse = lastResponse;
    }

    #region Pet creation - POST /pet

    // Repeated PhotoUrl / Tag rows build lists; 'null' leaves a field out. Prefix explicit Ids with {run_id_int}.
    [When(@"^pet is created with the following values via POST /pet$")]
    public void WhenPetIsCreated(Table table)
    {
        var response = _petApi.CreatePet(BuildPet(table));
        _lastResponse.Value = response;

        var createdPet = response.IsSuccessful ? response.Content<PetDto>() : null;
        if (createdPet?.Id != null)
            _createdPets.Value.Add(createdPet);
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
                case "id": pet.Id = long.Parse(value); break;
                case "name": pet.Name = value; break;
                case "status": pet.Status = value; break;
                case "category": pet.Category = new Category { Name = value }; break;
                case "photourl": pet.PhotoUrls.Add(value); break;
                case "tag": pet.Tags.Add(ParseTag(value)); break;
                default: throw new NotSupportedException($"Field '{row["Field"]}' is not supported.");
            }
        }

        return pet;
    }

    // "1:friendly" -> { Id = 1, Name = "friendly" }; "friendly" -> { Name = "friendly" }
    private static Tag ParseTag(string value)
    {
        var parts = value.Split(':', 2);
        return parts.Length == 2
            ? new Tag { Id = long.Parse(parts[0]), Name = parts[1].Trim() }
            : new Tag { Name = value.Trim() };
    }

    #endregion

    #region Pet retrieval - GET /pet/{id}

    // For pets the scenario did not create; the ID is sent as written, so 'abc' works too.
    [When(@"^the pet with ID '([^']+)' is retrieved via GET /pet/\{id\}$")]
    public void WhenThePetWithIdIsRetrieved(string petId) => GetPet(ReplaceVariables(petId));

    [When(@"^the created pet named '([^']+)' is retrieved via GET /pet/\{id\}$")]
    public void WhenTheCreatedPetNamedIsRetrieved(string name) => GetPet(_createdPets.Named(ReplaceVariables(name)).Id!.Value.ToString());

    [When(@"^the last created pet is retrieved via GET /pet/\{id\}$")]
    public void WhenTheLastCreatedPetIsRetrieved() => GetPet(_createdPets.LastId.ToString());

    private void GetPet(string petId) => _lastResponse.Value = _petApi.GetPetById(petId);

    #endregion

    #region Pet deletion - DELETE /pet/{id}

    [When(@"^the pet with ID '([^']+)' is deleted via DELETE /pet/\{id\}$")]
    public void WhenThePetWithIdIsDeleted(string petId) => DeletePet(ReplaceVariables(petId));

    [When(@"^the created pet named '([^']+)' is deleted via DELETE /pet/\{id\}$")]
    public void WhenTheCreatedPetNamedIsDeleted(string name) => DeletePet(_createdPets.Named(ReplaceVariables(name)).Id!.Value.ToString());

    [When(@"^the last created pet is deleted via DELETE /pet/\{id\}$")]
    public void WhenTheLastCreatedPetIsDeleted() => DeletePet(_createdPets.LastId.ToString());

    private void DeletePet(string petId) => _lastResponse.Value = _petApi.DeletePetById(petId);

    #endregion

    #region Pet response checks

    // Checks the listed fields only. 'null' = absent, '[]' = empty list, '{valid-number}' = any Id, tags as 'id:name'.
    [Then(@"^the response should have '(\d+)' status code and contain the following values$")]
    public void ThenTheResponseShouldContainTheFollowingValues(int statusCode, Table table)
    {
        var pet = ReadPet(statusCode);

        using var scope = new AssertionScope();
        foreach (var row in table.Rows)
        {
            var expected = row["Value"] == "null" ? null : ReplaceVariables(row["Value"]);

            switch (row["Field"].ToLowerInvariant())
            {
                case "id" when expected == "{valid-number}": pet.Id.Should().NotBeNull(); break;
                case "id": pet.Id.Should().Be(expected == null ? null : long.Parse(expected)); break;
                case "name": pet.Name.Should().Be(expected); break;
                case "status": pet.Status.Should().Be(expected); break;
                case "category": (pet.Category?.Name).Should().Be(expected); break;
                case "photourls": pet.PhotoUrls.Should().Equal(ExpectedList(expected)); break;
                case "tags": pet.Tags.Select(FormatTag).Should().Equal(ExpectedList(expected)); break;
                default: throw new NotSupportedException($"Field '{row["Field"]}' is not supported in assertions.");
            }
        }
    }

    // The whole pet must equal the created one, as the POST returned it.
    [Then(@"^the response should have '(\d+)' status code and match the created pet named '([^']+)'$")]
    public void ThenTheResponseShouldMatchTheCreatedPetNamed(int statusCode, string name) =>
        AssertMatches(statusCode, _createdPets.Named(ReplaceVariables(name)));

    [Then(@"^the response should have '(\d+)' status code and match the last created pet$")]
    public void ThenTheResponseShouldMatchTheLastCreatedPet(int statusCode) =>
        AssertMatches(statusCode, _createdPets.Last);

    [Then(@"^the created pets should have different IDs$")]
    public void ThenTheCreatedPetsShouldHaveDifferentIds() =>
        _createdPets.Value.Select(pet => pet.Id).Should().OnlyHaveUniqueItems();

    private void AssertMatches(int statusCode, PetDto expected) =>
        ReadPet(statusCode).Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());

    private PetDto ReadPet(int statusCode)
    {
        var response = _lastResponse.Value;
        ((int)response.StatusCode).Should().Be(statusCode, "response body: {0}", response.Content);
        return response.Content<PetDto>()
               ?? throw new InvalidOperationException("The response has no body, expected a pet.");
    }

    // "[]" -> empty list; "a ↕ b" -> ["a", "b"]
    private static List<string> ExpectedList(string? value) =>
        value == "[]" ? new List<string>() : value.SplitBySpecSymbol();

    // { Id = 1, Name = "friendly" } -> "1:friendly"
    private static string? FormatTag(Tag tag) => tag.Id == null ? tag.Name : $"{tag.Id}:{tag.Name}";

    #endregion
}
