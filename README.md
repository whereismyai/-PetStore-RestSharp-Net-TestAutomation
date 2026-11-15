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
dotnet test --filter "TestCategory!~bug"          # leave out every bug-linked scenario
dotnet test --filter "TestCategory~bug"           # only bug-linked scenarios
dotnet test --filter "TestCategory!=shared-ids"   # leave out boundary Ids other users may also use
dotnet test --filter "Name~Deleted"               # scenarios by (part of) their name
dotnet test --logger "trx"                        # TRX report with each scenario's log lines
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
  Extensions/                  StringExtensions (ToLong)
  Helpers/                     EnvFileHelper (.env settings), JsonHelper
  Tms/                         Bug, BugRepository (bugs/*.md), BugGate (skip decision)
WhereIsMyAi.PetStore.TestAutomation/
  Api/                         PetStoreConfig, Clients/PetApiClient, DTOs/PetDto
  RuntimeVariables/            per-scenario state: CreatedPets, LastResponse, RunIdInt
  Steps/                       BaseStep, CommonSteps, PetSteps (regions: creation, retrieval, deletion, checks)
  Hooks/                       BugHooks, CleanupHooks (after scenario)
  Tests/Features/              PetStore_Pet.feature
bugs/                          local bug repository: one BUG-xxx.md per bug
```

Dependencies point one way only: `Tests -> Api -> Core`.

## Approach

- **Check what is stored, not only the reply.** Every created pet is read back with `GET`. When the Id is known,
  the whole expected pet is written in the feature file and both responses must equal it exactly (all fields,
  category and tag ids, list order). Only pets with generated Ids are compared with the pet the `POST` returned.
  Names and tags are in English, Ukrainian, Spanish and French, with emoji and special characters.
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
  The local bug repository was added in a single prompt, only to show the mechanism. In a real project the
  hook would read the status of the blocking ticket from Jira instead of `bugs/*.md`, and run or skip the test
  depending on it.
- **One log per scenario.** Each request is logged as `METHOD resource -> status` on the test's own thread,
  so every scenario's report contains exactly its own requests and warnings.

## Coverage

Each scenario creates its own pets, checks the `POST` response and the `GET` read-back, and adds delete checks
where a pet already exists, so no pet is created only to be deleted. Where the Id is known, the full expected
pet is written in the feature file. Blocked scenarios assert the expected behaviour and are skipped while their
bug is open.

| # | Scenario | Result |
|---|---|---|
| S1 | A pet can be created with all fields and deleted | Pass |
| S2 | A pet can be created and deleted with each status value (3 examples) | Pass |
| S3 | A pet can be created and deleted with boundary values (4 examples) | Pass |
| S4 | Tags and photo URLs can be reused by several pets and stay after one pet is deleted | Pass |
| S5 | Pets created without an ID get their own generated IDs | Blocked: [BUG-002](bugs/BUG-002.md) |
| S6 | Creating a pet with an existing ID is rejected and the existing pet is kept | Blocked: [BUG-001](bugs/BUG-001.md) |
| S7 | An invalid pet is rejected and nothing is stored (2 examples) | Blocked: [BUG-003](bugs/BUG-003.md) |
| S8 | Getting a pet by a non-numeric ID is rejected as an invalid ID | Blocked: [BUG-004](bugs/BUG-004.md) |

### Coverage matrix

| Input | Equivalence class | Covered by | Status |
|---|---|---|---|
| `id` (body) | explicit, new | S1, S2, S4 | Pass |
| `id` (body) | boundaries: `1`, int64 maximum - 1 | S3 | Pass |
| `id` (body) | explicit, already used | S6 | Blocked: BUG-001 |
| `id` (body) | not sent (generated) | S5 | Blocked: BUG-002 |
| `id` (path) | stored pet | S1-S4 | Pass |
| `id` (path) | deleted pet | S1, S2 | Pass |
| `id` (path) | non-numeric | S8 | Blocked: BUG-004 |
| `name` | English, Ukrainian, emoji, digits and brackets | S1, S2, S4 | Pass |
| `name` | empty string (accepted: the spec sets no minimum length) | S3 | Pass |
| `name` | 256 characters | S3 | Pass |
| `name` | missing (required) | S7 | Blocked: BUG-003 |
| `status` | `available`, `pending`, `sold` | S2 | Pass |
| `status` | not sent | S3, S4 | Pass |
| `status` | not in the enum | S7 | Blocked: BUG-003 |
| `category` | with id / not sent | S1 / S2-S4 | Pass |
| `photoUrls` | several / empty / shared by two pets | S1 / S2, S3 / S4 | Pass |
| `tags` | several with ids; Ukrainian, Spanish, French and `& " % ,` | S1, S4 | Pass |
| `tags` | empty / shared by two pets | S2, S3 / S4 | Pass |
| `DELETE` | existing pet -> `200`, Id as message | S2, S3 (status only: S1, S4) | Pass |
| `DELETE` | already deleted -> `404`, empty body | S1 | Pass |
| `DELETE` | one of two pets sharing a tag and photo URL | S4 | Pass |

Not covered: Id `0` and negative Ids (the API replaces them with a generated Id; the spec defines no rule for
them, so there is nothing to assert), Ids beyond int64 (the client sends a `long`), the `api_key` header on `DELETE`, malformed JSON, `PUT /pet` and
`GET /pet/findByStatus`.

### Trade-offs

- Exact expected pets repeat the request table (three times in S1): verbose, but every checked value is
  visible in the feature file.
- Delete checks live inside creation scenarios: fewer pets on the shared server, but a failed delete also fails
  that scenario.
- Blocked scenarios are skipped, not run as known failures: no side effects on the shared server, but a fix is
  only noticed by running them with the gate off.
- `{run_id_int}` makes Id collisions on the public server unlikely, not impossible; there are no retries.
- A pet addressed by name in a step cannot have an apostrophe in its name.
- Boundary Ids (`1`, int64 maximum - 1) cannot take the `{run_id_int}` prefix, so they can collide with other
  users' pets on the public server. The outline is tagged `@shared-ids` to leave it out of unstable runs.

## Continuous integration

`.github/workflows/dotnet-desktop.yml` runs on every pull request into `main`:

- `build` - restores and builds the solution. Make it a required check (branch protection) to block merges.
- `test` - runs `dotnet test` after a successful build, with `BASE_URL` and `BUGS_REPO_PATH` set in the workflow so the open-bug skip logic works.
- Skipped scenarios stay visible: each one is a yellow warning annotation, and a separate grey `Skipped tests` check lists them (only created when something was skipped, so do not make it required; it needs a PR from this repo, not a fork).

## Test report

Run on 2026-11-15 against `https://petstore.swagger.io/v2`:

| Total | Passed | Failed | Skipped (open bug) | Duration |
|---|---|---|---|---|
| 14 | 9 | 0 | 5 | ~10 s |

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

- **More CI triggers.** The workflow runs on pull requests only; add on demand (`workflow_dispatch`) and a
  nightly `schedule` (cron) against the public server.
- **Reporting.** Publish the results of each run: TRX as a build artifact plus a test summary on the
  workflow and PR page. Then add an HTML report (e.g. Allure with its Reqnroll adapter) with each scenario's
  steps and log lines.
- **More endpoints.** Store (`GET /store/inventory`, `POST /store/order`, `GET /store/order/{id}`) and User
  (`POST /user`, `GET /user/{username}`, `PUT /user/{username}`). Each one is a new client, DTO, steps class
  and feature file.
- **More Pet cases.** `PUT /pet`, `GET /pet/findByStatus`, the `api_key` header on `DELETE`, and malformed
  JSON bodies.
