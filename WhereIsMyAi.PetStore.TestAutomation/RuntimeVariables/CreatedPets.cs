using System;
using System.Collections.Generic;
using System.Linq;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;

namespace WhereIsMyAi.PetStore.TestAutomation.RuntimeVariables;

/// <summary>
/// Runtime context variable: pets created by the current scenario, in creation order, as the POST response
/// returned them. Reqnroll DI creates one instance per scenario; the AfterScenario hook removes every pet
/// listed here from the backend.
/// Deleted pets stay listed so a scenario can still refer to them (e.g. check that GET now returns 404).
/// </summary>
public class CreatedPets
{
    public List<PetDto> Value { get; } = new();

    /// <summary>The most recently created pet ("the last created pet").</summary>
    public PetDto Last =>
        Value.LastOrDefault()
        ?? throw new InvalidOperationException("No pet has been created in this scenario yet.");

    /// <summary>Id of the most recently created pet.</summary>
    public long LastId => Last.Id!.Value;

    /// <summary>The pet created in this scenario with this name; the name must be unique within the scenario.</summary>
    public PetDto Named(string name)
    {
        var pets = Value.Where(pet => pet.Name == name).ToList();
        return pets.Count == 1
            ? pets[0]
            : throw new InvalidOperationException(pets.Count == 0
                ? $"No pet named '{name}' has been created in this scenario."
                : $"{pets.Count} pets named '{name}' were created in this scenario - give each pet a unique name.");
    }
}
