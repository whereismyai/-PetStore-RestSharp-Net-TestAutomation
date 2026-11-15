using System.Collections.Generic;
using WhereIsMyAi.PetStore.TestAutomation.Api.Data;
using WhereIsMyAi.PetStore.TestAutomation.Api.DTOs;
using WhereIsMyAi.PetStore.TestAutomation.Api.RequestBuilders.Base;
using RestSharp;

namespace WhereIsMyAi.PetStore.TestAutomation.Api.RequestBuilders;

/// <summary>Builds and sends /pet requests. WithX sets a field of the body; a field never set is not sent.</summary>
public class PetRequestBuilder(RestClient client) : PetStoreRequestBuilderBase<PetRequestBuilder>(client)
{
    private readonly PetDto _body = new();

    /// <summary>The pet built so far (also the expected pet in response checks).</summary>
    public PetDto Body => _body;

    public PetRequestBuilder WithId(long id)
    {
        _body.Id = id;
        return this;
    }

    public PetRequestBuilder WithName(string name)
    {
        _body.Name = name;
        return this;
    }

    public PetRequestBuilder WithStatus(string status)
    {
        _body.Status = status;
        return this;
    }

    public PetRequestBuilder WithCategory(long? id, string name)
    {
        _body.Category = new CategoryDto { Id = id, Name = name };
        return this;
    }

    /// <summary>Adds one photo URL; call again for more.</summary>
    public PetRequestBuilder WithPhotoUrl(string photoUrl)
    {
        (_body.PhotoUrls ??= new List<string>()).Add(photoUrl);
        return this;
    }

    /// <summary>Adds one tag; call again for more.</summary>
    public PetRequestBuilder WithTag(long? id, string name)
    {
        (_body.Tags ??= new List<TagDto>()).Add(new TagDto { Id = id, Name = name });
        return this;
    }

    public RestResponse SendCreatePet() => Send(Method.Post, Endpoints.Pet, _body);

    public RestResponse SendGetPetById(string petId) => Send(Method.Get, Endpoints.PetById(petId));

    public RestResponse SendDeletePetById(string petId) => Send(Method.Delete, Endpoints.PetById(petId));
}
