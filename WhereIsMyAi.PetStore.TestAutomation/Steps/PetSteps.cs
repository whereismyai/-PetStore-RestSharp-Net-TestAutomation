using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using WhereIsMyAi.PetStore.TestAutomation.Api.Clients;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs.RuntimeVariables;
using WhereIsMyAi.RestSharpBase.Core.Helpers;
using Reqnroll;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.Steps;

/// <summary>
/// Pet steps, one region per action: creation, retrieval, deletion, and the checks of a pet response.
/// </summary>
[Binding]
public class PetSteps
{
    private readonly PetApiClient _petApi;
    private readonly LastResponse _lastResponse;

    public PetSteps(PetApiClient petApi, LastResponse lastResponse)
    {
        _petApi = petApi;
        _lastResponse = lastResponse;
    }

    #region Pet creation - POST /pet

    // The action under test: the response is checked by the next Then step.
    // Repeated "PhotoUrl" / "Tag" rows accumulate into lists, so one table shape covers all cases.
    // A 'null' value leaves the field out (e.g. | Name | null | for a pet without the required name).
    [When(@"^pet is created with the following values via POST /pet$")]
    public void WhenPetIsCreated(Table table) => CreatePet(table);

    private RestResponse CreatePet(Table table)
    {
        var response = _petApi.CreatePet(BuildPet(table));
        _lastResponse.Value = response;
        return response;
    }

    private static PetDto BuildPet(Table table)
    {
        var pet = new PetDto();

        foreach (var row in table.Rows)
        {
            var value = row["Value"];
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

    [When(@"^the pet with ID '([^']+)' is retrieved via GET /pet/\{id\}$")]
    public void WhenThePetWithIdIsRetrieved(string petId) => GetPet(long.Parse(petId));

    private void GetPet(long petId) => _lastResponse.Value = _petApi.GetPetById(petId);

    #endregion

    #region Pet deletion - DELETE /pet/{id}

    [When(@"^the pet with ID '([^']+)' is deleted via DELETE /pet/\{id\}$")]
    public void WhenThePetWithIdIsDeleted(string petId) => DeletePet(long.Parse(petId));

    private void DeletePet(long petId) => _lastResponse.Value = _petApi.DeletePetById(petId);

    #endregion

    #region Pet response checks

    // Checks the listed fields only - use it where the expected values are the point of the scenario.
    // Special values in the Value column: 'null' = field is absent/null, '[]' = empty list.
    // Tags are written as on creation: 'id:name' for a tag with an id, 'name' without one.
    [Then(@"^the response should have '(\d+)' status code and contain the following values$")]
    public void ThenTheResponseShouldContainTheFollowingValues(int statusCode, Table table)
    {
        var pet = ReadPet(statusCode);

        // All mismatching fields are reported together, not just the first one.
        using var scope = new AssertionScope();
        foreach (var row in table.Rows)
        {
            var expected = row["Value"] == "null" ? null : row["Value"];

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
