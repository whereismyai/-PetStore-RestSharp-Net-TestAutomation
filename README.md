# PetStore Test Automation

BDD API test automation for the **Pet** endpoints of the public [Swagger PetStore](https://petstore.swagger.io)
API, written in C# / .NET 10 with Reqnroll (Gherkin, NUnit runner), RestSharp and FluentAssertions.

## The task

Automate the main Pet API flows - **create** (`POST /pet`), **read** (`GET /pet/{id}`) and **delete**
(`DELETE /pet/{id}`) - as readable Gherkin scenarios. They should check what the API really stores, not only
the status code, clean up after themselves on a shared public server, and document the API defects they find.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Internet access to `https://petstore.swagger.io`
- Optional: Rider or Visual Studio with the Reqnroll plugin (Gherkin highlighting, step navigation)

## Setup and run

```bash
git clone https://github.com/whereismyai/-PetStore-RestSharp-Net-TestAutomation.git
cd ./-PetStore-RestSharp-Net-TestAutomation
cp .env.example .env        # BASE_URL=https://petstore.swagger.io/v2, BUGS_REPO_PATH=bugs
dotnet test                 # restores, builds both projects and runs all scenarios
```

| `.env` key | Meaning |
|---|---|
| `BASE_URL` | API under test (required) |
| `BUGS_REPO_PATH` | local bug repository folder, relative to the `.env` folder (`bugs`). Optional: without it, bug-linked scenarios run instead of being skipped, and a warning is logged |

A real environment variable with the same name overrides the file (useful in CI).

```bash
dotnet test --filter "TestCategory!~bug"   # leave out every bug-linked scenario
dotnet test --filter "TestCategory~bug"    # only bug-linked scenarios
dotnet test --filter "Name~Deleted"        # scenarios by (part of) their name
dotnet test --logger "trx"                 # TRX report with each scenario's log lines
```

## Solution layout

| Project | What it contains |
|---|---|
| `WhereIsMyAi.RestSharpBase.Core` | Project-agnostic base: RestSharp client wrapper, logger, helpers and a small local TMS. It knows nothing about PetStore or any test framework, so other API test projects can reuse it. |
| `WhereIsMyAi.PetStore.TestAutomation` | Everything PetStore-specific: API client, DTOs, step definitions, hooks, scenario variables, feature files. References Core. |

```
WhereIsMyAi.RestSharpBase.Core/
  Http/RestApiClient.cs        abstract RestSharp wrapper (Get / Post / Put / Delete), logs every request
  Logger/InternalLogger.cs     console logger that writes on the test's own thread
  Helpers/                     EnvFile (.env settings), JsonHelper, StringExtensions, VariableReplacer
  Tms/                         Bug, BugRepository (bugs/*.md), BugGate (skip decision)
WhereIsMyAi.PetStore.TestAutomation/
  Api/                         PetStoreConfig, Clients/PetApiClient, DTOs/PetDto
  RuntimeVariables/            per-scenario state: CreatedPets, LastResponse, RunIdInt
  Steps/                       BaseStep, CommonSteps, PetSteps (regions: creation, retrieval, deletion, checks)
  Hooks/                       BugHooks, RunIdIntHooks (before scenario), CleanupHooks (after scenario)
  Utils/VariableRepository.cs  scenario variables: {run_id_int}
  Tests/Features/              PetStore_Pet.feature
bugs/                          local bug repository: one BUG-xxx.md per bug
```

Dependencies point one way only: `Tests -> Api -> Core`.

## Approach

- **Check what is stored, not only the reply.** Every created pet is read back with `GET`. When the Id is known,
  the whole expected pet is written in the feature file and both responses must equal it exactly (all fields,
  category and tag ids, list order). Only pets with generated Ids are compared with the pet the `POST` returned.
  Names and tags include non-Latin letters, emoji and special characters.
  Error replies are checked by their exact message, not only by the status code.
- **No collisions on a shared server.** Explicit IDs are prefixed with `{run_id_int}`, a random 4-digit
  number generated for each scenario (`{run_id_int}12345` -> `482112345`), in the request, the checks and the
  GET/DELETE steps. The number is logged at the start of the scenario, so the data of a failed run can be found.
- **Clean up always.** Every pet a scenario creates is recorded; `CleanupHooks` deletes all of them after
  the scenario, whether it passed or failed.
- **Readable steps.** Step text names the HTTP method and URL. A pet is addressed by ID, by name or as the last
  created pet.
- **Deletion inside the main scenarios.** Delete checks are added to scenarios that already create pets (their
  names say so), so no extra pets are created just to delete them.
- **Known bugs live next to the tests.** A scenario that documents an API bug asserts the *expected*
  behaviour and carries one tag, `@bug:<id>`. Before it starts, `BugHooks` reads `bugs/<id>.md`: while the bug
  is `Open` the scenario is skipped with the bug's title as the reason. Set `Status: Fixed` and it runs again,
  with no code change. To check for fixes: `BUGS_REPO_PATH=none dotnet test --filter "TestCategory~bug"`.
- **One log per scenario.** Each request is logged as `METHOD resource -> status` on the test's own thread,
  so every scenario's report contains exactly its own requests and warnings.

## Coverage matrix

| Endpoint | Case | Scenario | Result |
|---|---|---|---|
| `POST /pet` | All fields (Id, name, status, category, 2 photo URLs, 3 tags) stored as sent | A pet can be created with all fields and deleted | Pass |
| `POST /pet` | Every status value: `available`, `pending`, `sold` | A pet can be created and deleted with each status value | Pass (3) |
| `POST /pet` | Tags and photo URLs reused by two pets without affecting each other | Tags and photo URLs can be reused by several pets and stay after one pet is deleted | Pass |
| `POST /pet` | No Id -> a distinct generated Id per pet | Pets created without an ID get their own generated IDs | Skipped: [BUG-002](bugs/BUG-002.md) |
| `POST /pet` | Existing Id -> rejected, existing pet kept | Creating a pet with an existing ID is rejected and the existing pet is kept | Skipped: [BUG-001](bugs/BUG-001.md) |
| `POST /pet` | Missing `name`, unknown `status` -> `405`, nothing stored | An invalid pet is rejected and nothing is stored | Skipped (2): [BUG-003](bugs/BUG-003.md) |
| `GET /pet/{id}` | Existing pet returned unchanged | covered by every POST scenario | Pass |
| `GET /pet/{id}` | Deleted pet -> `404 Pet not found` | A pet can be created and deleted with each status value | Pass (3) |
| `GET /pet/{id}` | Non-numeric Id -> `400 Invalid ID supplied` | Getting a pet by a non-numeric ID is rejected as an invalid ID | Skipped: [BUG-004](bugs/BUG-004.md) |
| `DELETE /pet/{id}` | Existing pet -> `200`, Id as message | A pet can be created and deleted with each status value | Pass (3) |
| `DELETE /pet/{id}` | Already deleted -> `404`, empty body | A pet can be created with all fields and deleted | Pass |
| `DELETE /pet/{id}` | Deleting one pet keeps the shared tag and photo URL on another | Tags and photo URLs can be reused by several pets and stay after one pet is deleted | Pass |

Not automated on purpose: `DELETE` with/without the optional `api_key` header, and malformed JSON bodies
(they would need a raw-body client method).

## Test report

Run on 2026-11-15 against `https://petstore.swagger.io/v2`:

| Total | Passed | Failed | Skipped (open bug) | Duration |
|---|---|---|---|---|
| 10 | 5 | 0 | 5 | ~10 s |

All 5 skipped scenarios were also run on purpose (with `BUGS_REPO_PATH` pointing to a missing folder). Each
failed exactly as its bug describes:

| Bug | Expected | Actual |
|---|---|---|
| [BUG-001](bugs/BUG-001.md) | `409`, existing pet unchanged | `200`, the existing pet is silently overwritten |
| [BUG-002](bugs/BUG-002.md) | a distinct generated Id per pet | every pet gets `9223372036854775807` and overwrites the previous one |
| [BUG-003](bugs/BUG-003.md) | `405 Invalid input`, nothing stored | `200`, a pet without `name` or with `status: flying` is stored |
| [BUG-004](bugs/BUG-004.md) | `400 Invalid ID supplied` | `404` with `java.lang.NumberFormatException ...` as the message |

## AI assistance

`WhereIsMyAi.RestSharpBase.Core` started from my own RestSharp template. The rest was built with the help of
an AI coding assistant (Claude Code) - code, scenarios and documentation - under my direction and review.
Every scenario and every bug report was run and verified against the live API.

## Future to-dos

- **Run the tests in GitHub Actions.** Add a workflow that runs `dotnet test` on every push and pull request,
  on demand (`workflow_dispatch`), and on a nightly `schedule` (cron) against the public server. `BASE_URL`
  and `BUGS_REPO_PATH` come from workflow environment variables.
- **Reporting.** Publish the results of each run: TRX as a build artifact plus a test summary on the
  workflow and PR page. Then add an HTML report (e.g. Allure with its Reqnroll adapter) with each scenario's
  steps and log lines.
- **More endpoints.** Store (`GET /store/inventory`, `POST /store/order`, `GET /store/order/{id}`) and User
  (`POST /user`, `GET /user/{username}`, `PUT /user/{username}`). Each one is a new client, DTO, steps class
  and feature file.
- **More Pet cases.** `PUT /pet`, `GET /pet/findByStatus`, the `api_key` header on `DELETE`, and malformed
  JSON bodies.
