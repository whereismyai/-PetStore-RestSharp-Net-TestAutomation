# PetStore Test Automation

Automated API tests for the **Pet** endpoints of the public [Swagger PetStore](https://petstore.swagger.io) API:
`POST /pet`, `GET /pet/{id}` and `DELETE /pet/{id}`. The scenarios are written in Gherkin, so you can read what
each test checks without reading any C#.

Stack: C# / .NET 10, Reqnroll (Gherkin, NUnit runner), RestSharp, FluentAssertions.

## How to read these docs

The documentation is split into three files. Start with the one that answers your question:

| File | Answers | Best for |
|---|---|---|
| [TEST_RESULTS.md](TEST_RESULTS.md) | What passes today, what is covered, which API bugs were found | Tech PMs, QA leads: start here |
| [TEST_APPROACH.md](TEST_APPROACH.md) | How the tests are designed and why: atomic scenarios, bug handling, safety on a shared server, trade-offs | QA engineers reviewing or extending the suite |
| README.md (this file) | How to clone, set up and run the tests, what CI does, how the code is organised | Anyone running the suite or changing the code |

To see what a single test does, open the scenario in its feature file:
[`PetStore_Pet_E2E.feature`](WhereIsMyAi.PetStore.TestAutomation/Tests/Features/PetStore_Pet_E2E.feature) (what a valid pet stores),
[`PetStore_Pet_Validation.feature`](WhereIsMyAi.PetStore.TestAutomation/Tests/Features/PetStore_Pet_Validation.feature) (accepted and rejected input),
[`PetStore_Pet_Lifecycle.feature`](WhereIsMyAi.PetStore.TestAutomation/Tests/Features/PetStore_Pet_Lifecycle.feature) (delete, missing pets, reused and duplicate Ids) or
[`PetStore_Pet_Http.feature`](WhereIsMyAi.PetStore.TestAutomation/Tests/Features/PetStore_Pet_Http.feature) (`Content-Type`, `Accept`, `api_key`). Each bug's full report
is in [`Bugs/`](Bugs).

## The task

Automate the main Pet flows (create, read and delete) as readable Gherkin scenarios. The tests must check
what the API actually stores, not only the status code. They must clean up after themselves on a shared public
server and document every API defect they find. Time box: 2 hours.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Internet access to `https://petstore.swagger.io`
- Optional: Rider or Visual Studio with the Reqnroll plugin (Gherkin highlighting, step navigation)

## Setup and run

```bash
git clone https://github.com/whereismyai/PetStore-RestSharp-Net-TestAutomation.git
cd ./PetStore-RestSharp-Net-TestAutomation
cp .env.example .env        # BASE_URL=https://petstore.swagger.io/v2, BUGS_REPO_PATH=Bugs
dotnet test                 # restores, builds, runs the Core unit tests and all scenarios
```

Settings live in `.env` in the repository root. A real environment variable with the same name overrides
the file, which is how CI sets them.

| Key | Required | What it does |
|---|---|---|
| `BASE_URL` | Yes | The API under test |
| `BUGS_REPO_PATH` | No | Folder of the local bug repository, relative to `.env` (`Bugs`). Without it, bug-linked scenarios run instead of being skipped, and a warning is logged |

Common runs:

```bash
dotnet test --filter "TestCategory=smoke"                 # smoke suite: the 8 core cases, ~6 s
dotnet test --filter "TestCategory=e2e"                   # one feature file: e2e, validation, lifecycle or http
dotnet test --filter "TestCategory!~bug"                  # skip every bug-linked scenario
dotnet test --filter "TestCategory~bug"                   # only bug-linked scenarios
BUGS_REPO_PATH=none dotnet test --filter "TestCategory~bug"   # run bug-linked scenarios to check for fixes
dotnet test --filter "TestCategory!=shared-ids"           # leave out boundary Id 1, which other users may also use
dotnet test --filter "Name~Deleted"                       # scenarios whose name contains a word
dotnet test --logger "trx"                                # TRX report with each scenario's log lines
```

A normal run is green: tests linked to an open bug are skipped, not failed. Why, and how to change a bug's
status, is explained in [TEST_APPROACH.md](TEST_APPROACH.md#bugs-are-covered-at-the-level-we-can-reach).

## Continuous integration

`.github/workflows/ci.yml` runs on every pull request into `main`:

- **`build`** restores and builds the solution, then runs the Core unit tests (no network). It is the
  required check: a PR that does not build or breaks Core cannot be merged.
- **`test`** runs the PetStore scenarios after a successful build, with `BASE_URL` and `BUGS_REPO_PATH` set in
  the workflow. It is deliberately not required: the tests hit a shared public server that the PR does not
  control, so a red run is shown on the PR but does not block the merge.
- **Skipped scenarios stay visible.** Each one becomes a warning annotation with its bug's id and title, and a
  grey `Skipped tests` check lists them all (only for PRs from this repository; a fork's token cannot create
  checks).

## Solution layout

| Project | What it contains |
|---|---|
| `WhereIsMyAi.RestSharpBase.Core` | Project-agnostic base: RestSharp client wrapper, logger, helpers and a small local TMS. It knows nothing about PetStore or any test framework, so other API test projects can reuse it. |
| `WhereIsMyAi.PetStore.TestAutomation` | Everything PetStore-specific: API client, DTOs, step definitions, hooks, scenario variables, feature files. References Core. |
| `WhereIsMyAi.RestSharpBase.Core.UnitTests` | Unit tests for Core: request building, transport failure, malformed JSON, `.env` precedence, bug-gate decisions, a valid `Status` in every `Bugs/*.md`. Fake HTTP handler, no network. A smoke check made entirely with AI assistance, outside the 2-hour scope (see [AI assistance](#ai-assistance)). |

```
WhereIsMyAi.RestSharpBase.Core/
  Http/RestApiClient.cs        abstract RestSharp wrapper: takes your RestClient (auth, headers, timeouts) and logger;
                               Get / Post / Put / Delete, Execute(request) for anything else; logs every request;
                               disposes the RestClient it was given
  Logger/InternalLogger.cs     console logger that writes on the test's own thread
  Extensions/                  StringExtensions (ToLong)
  Helpers/                     EnvFileHelper (.env settings), JsonHelper
  Tms/                         Bug, BugRepository (Bugs/*.md), BugGate (skip decision)
WhereIsMyAi.PetStore.TestAutomation/
  Api/                         PetStoreConfig, Clients/PetApiClient, DTOs/PetDto, CategoryDto, TagDto
  RuntimeVariables/            per-scenario state: CreatedPets, LastResponse, RunIdInt
  Steps/                       BaseStep, CommonSteps, PetSteps (regions: preconditions, creation, retrieval, deletion, checks)
  Hooks/                       BugHooks, CleanupHooks (after scenario)
  Tests/Features/              PetStore_Pet_E2E, _Validation, _Lifecycle, _Http (.feature)
WhereIsMyAi.RestSharpBase.Core.UnitTests/   unit tests per Core folder (FakeHttpHandler)
Bugs/                          local bug repository: one BUG-xxx.md per bug
```

Dependencies point one way only: `Tests -> Api -> Core`.

## AI assistance

`WhereIsMyAi.RestSharpBase.Core` started from my own RestSharp template. I built the tests and their
infrastructure with the help of an AI coding assistant (Claude Code). I made every strategic decision (test
strategy, architecture, coverage scope, how known bugs are handled), reviewed every line of code, and I am
accountable for all of it. Every scenario and every bug report was run and verified against the live API.

The Core unit tests (`WhereIsMyAi.RestSharpBase.Core.UnitTests`) go beyond the 2-hour task. They were created
entirely with AI assistance, only as a smoke check of the reusable Core, so that the required CI check runs
something deterministic. The PetStore scenarios are the actual deliverable.

## Next steps

- **More CI triggers.** The workflow runs on pull requests only. Add on-demand runs (`workflow_dispatch`) and a
  nightly `schedule` (cron) against the public server.
- **Reporting.** Publish each run's results: TRX as a build artifact, plus a test summary on the workflow and
  PR page. Then add an HTML report (for example, Allure with its Reqnroll adapter) with each scenario's steps
  and log lines.
- **More endpoints.** Store (`GET /store/inventory`, `POST /store/order`, `GET /store/order/{id}`) and User
  (`POST /user`, `GET /user/{username}`, `PUT /user/{username}`). Each one is a new client, DTO, steps class
  and feature file.
- **More Pet cases.** `PUT /pet`, `GET /pet/findByStatus`, the `api_key` header on `DELETE`, and malformed
  JSON bodies.
