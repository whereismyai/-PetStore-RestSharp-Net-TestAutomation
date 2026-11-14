using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using WhereIsMyAi.PetStore.TestAutomation.Api.Clients;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs.RuntimeVariables;
using WhereIsMyAi.RestSharpBase.Core.Helpers;
using WhereIsMyAi.PetStore.TestAutomation.Utils;
using Reqnroll;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.Steps;

/// <summary>
/// Pet steps, one region per action: creation, retrieval, deletion, and the checks of a pet response.
/// Retrieval and deletion each have the same three ways to name the pet:
/// by ID ('{run_id_int}12345'), by name ('Rex' - a pet created in this scenario), or the last created pet.
/// </summary>
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

    // The action under test: the response is checked by the next Then step.
    // Repeated "PhotoUrl" / "Tag" rows accumulate into lists, so one table shape covers all cases.
    // A 'null' value leaves the field out (e.g. | Name | null | for a pet without the required name).
    // Recommended: give an explicit Id the {run_id_int} prefix - | Id | {run_id_int}12345 | - so a rerun never
    // collides with a pet left behind by an earlier run. Leave Id out to let the backend generate it.
    // Recommended: give every pet of a scenario its own name, so later steps can refer to it by name.
    [When(@"^pet is created with the following values via POST /pet$")]
    public void WhenPetIsCreated(Table table) => CreatePet(table);

    // A precondition: same table as above, but fails right away if the POST is not successful.
    [Given(@"^pet is created with the following values via POST /pet$")]
    public void GivenPetIsCreated(Table table)
    {
        var response = CreatePet(table);
        response.IsSuccessful.Should().BeTrue("the precondition POST /pet must succeed, response: {0}", response.Content);
    }

    // Sends the pet and remembers it (as the POST returned it) for later steps and for cleanup.
    private RestResponse CreatePet(Table table)
    {
        var response = _petApi.CreatePet(BuildPet(table));
        _lastResponse.Value = response;

        var createdPet = response.IsSuccessful ? response.Content<PetDto>() : null;
        if (createdPet?.Id != null)
            _createdPets.Value.Add(createdPet);

        return response;
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

    // Recommended ID: the one used on creation - '{run_id_int}12345' for an explicit Id,
    // '{created-id:1}' (1st pet of the scenario, 2 = 2nd, ...) for a backend-generated one.
    // Use it for pets the scenario did not create (a pet that never existed, a rejected one).
    [When(@"^the pet with ID '([^']+)' is retrieved via GET /pet/\{id\}$")]
    public void WhenThePetWithIdIsRetrieved(string petId) => GetPet(long.Parse(ReplaceVariables(petId)));

    // A pet created earlier in this scenario, by its name (the name must be unique within the scenario).
    [When(@"^the created pet named '([^']+)' is retrieved via GET /pet/\{id\}$")]
    public void WhenTheCreatedPetNamedIsRetrieved(string name) => GetPet(_createdPets.Named(ReplaceVariables(name)).Id!.Value);

    [When(@"^the last created pet is retrieved via GET /pet/\{id\}$")]
    public void WhenTheLastCreatedPetIsRetrieved() => GetPet(_createdPets.LastId);

    private void GetPet(long petId) => _lastResponse.Value = _petApi.GetPetById(petId);

    #endregion

    #region Pet deletion - DELETE /pet/{id}

    // Recommended ID: as for GET - '{run_id_int}12345' (explicit Id) or '{created-id:N}' (generated Id).
    [When(@"^the pet with ID '([^']+)' is deleted via DELETE /pet/\{id\}$")]
    public void WhenThePetWithIdIsDeleted(string petId) => DeletePet(long.Parse(ReplaceVariables(petId)));

    // A pet created earlier in this scenario, by its name (the name must be unique within the scenario).
    [When(@"^the created pet named '([^']+)' is deleted via DELETE /pet/\{id\}$")]
    public void WhenTheCreatedPetNamedIsDeleted(string name) => DeletePet(_createdPets.Named(ReplaceVariables(name)).Id!.Value);

    [When(@"^the last created pet is deleted via DELETE /pet/\{id\}$")]
    public void WhenTheLastCreatedPetIsDeleted() => DeletePet(_createdPets.LastId);

    private void DeletePet(long petId) => _lastResponse.Value = _petApi.DeletePetById(petId);

    #endregion

    #region Pet response checks

    // Checks the listed fields only - use it where the expected values are the point of the scenario.
    // Special values in the Value column: 'null' = field is absent/null, '[]' = empty list.
    // Tags are written as on creation: 'id:name' for a tag with an id, 'name' without one.
    // Recommended for Id: always check the exact value - '{run_id_int}12345' when the scenario set the Id,
    // '{created-id:N}' (N-th pet created in the scenario) when the backend generated it.
    [Then(@"^the response should have '(\d+)' status code and contain the following values$")]
    public void ThenTheResponseShouldContainTheFollowingValues(int statusCode, Table table)
    {
        var pet = ReadPet(statusCode);

        // All mismatching fields are reported together, not just the first one.
        using var scope = new AssertionScope();
        foreach (var row in table.Rows)
        {
            var expected = row["Value"] == "null" ? null : ReplaceVariables(row["Value"]);

            switch (row["Field"].ToLowerInvariant())
            {
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

    // Whole-object check: the returned pet must equal the created pet (as the POST returned it) in every
    // field, including ids of the category and tags, in the same list order. Use it for read-backs: it proves
    // that what was stored is what was created, without repeating the expected values table.
    [Then(@"^the response should have '(\d+)' status code and match the created pet named '([^']+)'$")]
    public void ThenTheResponseShouldMatchTheCreatedPetNamed(int statusCode, string name) =>
        AssertMatches(statusCode, _createdPets.Named(ReplaceVariables(name)));

    // As above, for the last created pet.
    [Then(@"^the response should have '(\d+)' status code and match the last created pet$")]
    public void ThenTheResponseShouldMatchTheLastCreatedPet(int statusCode) =>
        AssertMatches(statusCode, _createdPets.Last);

    private void AssertMatches(int statusCode, PetDto expected) =>
        ReadPet(statusCode).Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());

    // Asserts the status code, then returns the body as a pet.
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

    // Same notation as the Tag rows on creation: { Id = 1, Name = "friendly" } -> "1:friendly".
    private static string? FormatTag(Tag tag) => tag.Id == null ? tag.Name : $"{tag.Id}:{tag.Name}";

    #endregion
}
