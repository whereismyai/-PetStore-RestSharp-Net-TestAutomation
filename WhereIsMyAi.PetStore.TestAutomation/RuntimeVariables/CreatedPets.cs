using System;
using System.Collections.Generic;
using System.Linq;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;

namespace WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;

/// <summary>Runtime variable: pets created by the current scenario, as POST returned them; deleted pets stay listed.</summary>
public class CreatedPets
{
    public List<PetDto> Value { get; } = new();

    public PetDto GetLastPet() =>
        Value.LastOrDefault() ?? throw new InvalidOperationException("No pet has been created in this scenario yet.");

    public long GetLastPetId() =>
        GetLastPet().Id ?? throw new InvalidOperationException("The last created pet has no Id.");

    /// <summary>The name must be unique within the scenario.</summary>
    public PetDto GetPetByName(string name)
    {
        var pets = Value.Where(pet => pet.Name == name).ToList();
        return pets.Count == 1
            ? pets[0]
            : throw new InvalidOperationException(pets.Count == 0
                ? $"No pet named '{name}' has been created in this scenario. Created so far: {GetCreatedPetNames()}."
                : $"{pets.Count} pets named '{name}' were created in this scenario - give each pet a unique name.");
    }

    public long GetPetIdByName(string name) =>
        GetPetByName(name).Id ?? throw new InvalidOperationException($"The pet named '{name}' has no Id.");

    private string GetCreatedPetNames() =>
        Value.Count == 0 ? "none" : string.Join(", ", Value.Select(pet => $"'{pet.Name}'"));
}
