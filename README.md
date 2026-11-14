# PetStore Test Automation

BDD API tests for the [Swagger PetStore](https://petstore.swagger.io) API: C# / .NET 10, Reqnroll (NUnit), RestSharp, FluentAssertions.

```bash
cp .env.example .env      # BASE_URL=https://petstore.swagger.io/v2
dotnet test
```

- `WhereIsMyAi.RestSharpBase.Core` - project-agnostic base: RestSharp client wrapper, logger, helpers.
- `WhereIsMyAi.PetStore.TestAutomation` - PetStore API clients, DTOs, step definitions and feature files.
