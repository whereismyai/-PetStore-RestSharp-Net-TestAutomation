# PetStore Test Automation

Automated API tests for the **Pet** endpoints of the public [Swagger PetStore](https://petstore.swagger.io) API.
The scenarios are written in Gherkin, so you can read what each test checks without reading any C#.

Stack: C# / .NET 10, Reqnroll (Gherkin, NUnit runner), RestSharp, FluentAssertions.

## At a glance

| | |
|---|---|
| Endpoints under test | `POST /pet`, `GET /pet/{id}`, `DELETE /pet/{id}` |
| Scenarios | 12, which expand to 23 test cases (one per `Examples` row) |
| Last run (2026-11-16) | 9 passed, 1 failed (intermittent BUG-002 reproduced), 13 skipped because of open bugs |
| API bugs found | 5, all open: [BUG-001](Bugs/BUG-001.md) to [BUG-005](Bugs/BUG-005.md) |
| Core unit tests | 43, all passing, no network |

## The task

Automate the main Pet flows (create, read and delete) as readable Gherkin scenarios. The tests must check
what the API actually stores, not only the status code. They must clean up after themselves on a shared public
server and document every API defect they find. Time box: 2 hours.

## Quick start

You need:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Internet access to `https://petstore.swagger.io`
- Optional: Rider or Visual Studio with the Reqnroll plugin (Gherkin highlighting, step navigation)

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
dotnet test --filter "TestCategory!~bug"          # skip every bug-linked scenario
dotnet test --filter "TestCategory~bug"           # only bug-linked scenarios
dotnet test --filter "TestCategory!=shared-ids"   # leave out boundary Id 1, which other users may also use
dotnet test --filter "Name~Deleted"               # scenarios whose name contains a word
dotnet test --logger "trx"                        # TRX report with each scenario's log lines
```

## Test approach

### A small, atomic suite

Most equivalence classes and boundaries belong in the backend's own unit and integration tests. This project
has no access to the backend or its tests, so the suite covers them from the outside, with as few end-to-end
scenarios as possible:

- **One behaviour per scenario.** When a scenario fails, its name tells you what broke.
- **No duplicated checks.** Steps inside a scenario build on each other (create, read back, delete, read
  again), but no scenario repeats another one's checks. Every `Examples` row adds a new equivalence class or
  boundary.

### Bugs are covered at the level we can reach

When the suite finds a defect, we cover it at the level available to us: the end-to-end API. Each bug gets a
file in `Bugs/` and its own scenario that:

- asserts the **expected** behaviour, not the current broken one;
- carries exactly one tag, `@bug:<id>`;
- is **skipped** while the bug is `Open`, and runs again on its own once the bug is `Fixed` or `Closed`.

That is why 13 of the 23 test cases are skipped today. They are not missing or broken tests: they are the
acceptance checks for 4 open bugs. A skipped test means "this behaviour is not verified on the current build",
and its skip reason names the bug.

How the skip works: before a bug-linked scenario starts, `BugHooks` reads `Status:` from `Bugs/<id>.md`.

| Status | Result |
|---|---|
| `Open` | Skipped, with `Blocked by open bug <id>: <title>` as the reason |
| `Open`, scenario also tagged `@intermittent` | Runs (see below) |
| `Fixed` or `Closed` | Runs, with no code change |
| Anything else | Runs with a warning; a Core unit test fails the build |

### Intermittent bugs keep running

Some bugs come and go. [BUG-002](Bugs/BUG-002.md) reproduced on three days out of four, and we cannot see why:
the server is a black box to us and is shared with other users. Skipping its scenario would collect no
evidence, and closing the bug as Cannot reproduce would report a data-loss defect as resolved. So the bug stays
`Open` and its scenario (S5) carries a second tag, `@intermittent`: it runs on every build, passes when the bug
does not occur and fails when it does. Each failure is logged with its time and Ids, which is evidence for the
bug. The scenario only runs in the non-blocking CI `test` job, so it never blocks a merge.

We deliberately do not call it `@flaky`. "Flaky" means the test is unreliable; this test is reliable, and the
API is not. A flaky label invites people to rerun or ignore a failure, which is wrong for a real defect.

### Raising and closing a bug

1. Add `Bugs/<id>.md`; the file name is the bug id. Only three lines are read, and the rest is free text for
   steps to reproduce, expected and actual results:

   ```markdown
   - Id: BUG-006
   - Status: Open
   - Title: <short title>
   ```

2. Tag the scenario that asserts the expected behaviour with `@bug:BUG-006`.
3. When the API is fixed, set `Status: Fixed`. A bug that stops reproducing without a known fix becomes
   `Status: Closed` with a `Resolution: Cannot reproduce` line. If it comes back on some runs only, reopen it
   and add `@intermittent` to its scenario.

To check whether the bugs are fixed, run them with the gate off:
`BUGS_REPO_PATH=none dotnet test --filter "TestCategory~bug"`.

The local bug repository is a stand-in, added in a single prompt only to show the mechanism. In a real project
the hook would read the status of the blocking ticket from Jira instead of `Bugs/*.md`.

### What the checks verify

- **What is stored, not only the reply.** In the scenarios that create pets to check them (S1-S5), every pet's
  `POST` reply and its `GET` read-back are compared with the whole expected pet, written in the feature file.
  With a known Id, both must match it exactly: all fields, category and tag ids, list order. Pets with
  generated Ids must match everything except the Id. The expected pet is never taken from an earlier response,
  because that response could repeat a wrong value.
- **Missing fields fail by name.** The response DTO is all-nullable, so a missing `id`, `name`, `photoUrls`
  or `tags` fails with that field's name instead of silently becoming a default like `[]`.
- **One fault per negative case.** Every pet is sent with its required fields (`name`, `photoUrls`) unless
  that field is under test, so an invalid-input scenario has exactly one thing wrong.
- **Exact error messages.** Error replies are checked by status code and by the reply's `message` field,
  compared exactly. The expected message is the spec's description of that response (`Pet not found`,
  `Invalid ID supplied`, `Invalid input`). Two exceptions: the spec defines no code for a duplicate Id, so
  `409` is an assumption (BUG-001); S1's repeated `DELETE` checks the status only, because the missing message
  is BUG-005 (S12).
- **Varied test data.** Names in English and Ukrainian, with emoji, digits and brackets; tags add Spanish,
  French and `& " % ,`.

### Safe on a shared server

- **No Id collisions.** Explicit Ids are prefixed with `{run_id_int}`, a random 4-digit number per scenario
  (`{run_id_int}12345` becomes `482112345`), in the request, the checks and the `GET`/`DELETE` steps. The
  number is logged when the scenario starts, so you can find a failed run's data on the server.
- **Always cleans up.** Every pet a scenario creates is recorded, both with the Id the reply returned and the
  Id that was sent (in case the reply is wrong or a rejected pet was stored anyway). `CleanupHooks` deletes all
  of them after the scenario, whether it passed or failed.
- **No pets created only to be deleted.** Delete checks are part of scenarios that already create pets, and
  those scenario names say so.

### Easy to read and debug

- **Readable steps.** Step text names the HTTP method and URL. A pet is addressed by Id, by name or as the
  last created pet.
- **One log per scenario.** Each request is logged as `Post /pet -> 200`, with the request body for `POST`
  and `PUT`, on the test's own thread. Every scenario's report contains exactly its own requests and warnings.
- **Ready for parallel runs, run serially.** All scenario state comes from Reqnroll DI, one instance per
  scenario; nothing is static. Parallel runs are not enabled, but a trial with 6 scenarios at once passed with
  correct per-scenario logs.

## Coverage

S1-S4 check behaviour that works today, and they pass. S5-S12 each assert the expected behaviour for one open
bug. S6-S12 are skipped until their bug is fixed; S5 runs on every build because BUG-002 is intermittent.

| # | Scenario | Test cases | Result |
|---|---|---|---|
| S1 | A pet can be created with all fields and deleted only once | 1 | Pass |
| S2 | A pet can be created and deleted with each status value | 3 | Pass |
| S3 | A pet can be created and deleted with boundary Ids and name lengths | 4 | Pass |
| S4 | Tags and photo URLs can be shared by two pets and stay after one pet is deleted | 1 | Pass |
| S5 | Pets created without an Id or with Id 0 get their own generated Ids | 1 | Runs, fails while [BUG-002](Bugs/BUG-002.md) reproduces (`@intermittent`) |
| S6 | Creating a pet with an existing Id is rejected with 409 Conflict | 1 | Skipped: [BUG-001](Bugs/BUG-001.md) |
| S7 | Creating a pet with an existing Id keeps the existing pet | 1 | Skipped: [BUG-001](Bugs/BUG-001.md) |
| S8 | An invalid pet is rejected with 405 Invalid input | 3 | Skipped: [BUG-003](Bugs/BUG-003.md) |
| S9 | An invalid pet is not stored | 3 | Skipped: [BUG-003](Bugs/BUG-003.md) |
| S10 | Getting a pet by an invalid Id is rejected with 400 Invalid ID supplied | 2 | Skipped: [BUG-004](Bugs/BUG-004.md) |
| S11 | Deleting a pet by an invalid Id is rejected with 400 Invalid ID supplied | 2 | Skipped: [BUG-004](Bugs/BUG-004.md) |
| S12 | Deleting a pet that does not exist is answered with 404 Pet not found | 1 | Skipped: [BUG-005](Bugs/BUG-005.md) |
| | **Total** | **23** | **9 pass, 1 intermittent, 13 skipped** |

### Coverage matrix

Each row is one equivalence class or boundary. **Pass** means it is verified on every run. **Intermittent**
means it is checked on every run, but the check fails whenever its bug reproduces. **Blocked** means a scenario
covers it, but that scenario is skipped while its bug is open, so the class is not verified on the current build.

| Input | Equivalence class or boundary | Covered by | Status |
|---|---|---|---|
| `id` (body) | explicit, new | S1, S2, S4 | Pass |
| `id` (body) | boundaries: `1` (smallest Id stored as sent), 18 digits (above 2^53) | S3 | Pass |
| `id` (body) | explicit, already used | S6, S7 | Blocked: BUG-001 |
| `id` (body) | not sent / `0` (the server generates a distinct Id) | S5 | Intermittent: BUG-002 |
| `id` (path) | existing pet | S1-S4 | Pass |
| `id` (path) | deleted pet: `GET` -> `404 Pet not found` | S2 | Pass |
| `id` (path) | non-numeric: `GET` / `DELETE` | S10 / S11 | Blocked: BUG-004 |
| `id` (path) | int64 maximum + 1: `GET` / `DELETE` | S10 / S11 | Blocked: BUG-004 |
| `name` | English, Ukrainian, emoji, digits and brackets | S1, S2, S4 | Pass |
| `name` | empty string (accepted: the spec sets no minimum length) | S3 | Pass |
| `name` | 256 characters | S3 | Pass |
| `name` | missing (required) | S8, S9 | Blocked: BUG-003 |
| `status` | `available`, `pending`, `sold` | S2 | Pass |
| `status` | not sent | S3, S4 | Pass |
| `status` | not in the enum | S8, S9 | Blocked: BUG-003 |
| `category` | with id / not sent | S1 / S2-S4 | Pass |
| `category` | name only (id comes back as `0`) | S5 | Intermittent: BUG-002 |
| `photoUrls` | one / several / shared by two pets | S2, S3 / S1 / S4 | Pass |
| `photoUrls` | missing (required) | S8, S9 | Blocked: BUG-003 |
| `tags` | several with ids; Ukrainian, Spanish, French and `& " % ,` | S1, S4 | Pass |
| `tags` | not sent / shared by two pets | S2, S3 / S4 | Pass |
| `DELETE` | existing pet -> `200`, Id as message | S2, S3 (status only: S1, S4) | Pass |
| `DELETE` | already deleted -> `404` (status only) | S1 | Pass |
| `DELETE` | never existed -> `404 Pet not found` | S12 | Blocked: BUG-005 |
| `DELETE` | one of two pets that share a tag and photo URL | S4 | Pass |

In total: 25 rows: 16 pass, 2 intermittent, 7 blocked by open bugs.

Not covered: negative Ids (the API treats them like `0` and generates an Id; the spec defines no rule for
them), body Ids beyond int64 (the client sends a `long`), an empty `photoUrls` list, the `api_key` header on
`DELETE`, malformed JSON, `PUT /pet` and `GET /pet/findByStatus`.

### Trade-offs

- **Verbose expected pets.** Each expected pet repeats the request table (three times in S1), but every
  checked value is visible in the feature file.
- **Delete checks inside creation scenarios.** Fewer pets on the shared server, but a failed delete also
  fails that scenario.
- **Blocked scenarios are skipped, not run as known failures.** No side effects on the shared server, but a
  fix is only noticed when you run them with the gate off.
- **The intermittent scenario runs and can fail.** S5 collects evidence for BUG-002, but `dotnet test` exits
  with a failure whenever the bug reproduces, and the CI `test` job turns red. When it does, the bug can
  overwrite another user's pet at the int64 maximum, and cleanup then deletes that pet. Acceptable on a
  public demo server, as for Id `1`.
- **Collisions are unlikely, not impossible.** `{run_id_int}` makes Id collisions on the public server
  unlikely; there are no retries.
- **Boundary Id `1` is shared.** It cannot take the `{run_id_int}` prefix. Because `POST` overwrites
  (BUG-001), the scenario replaces and then deletes another user's pet `1`, if there is one. That is
  acceptable on a public demo server. Only that example is tagged `@shared-ids` (its own `Examples` block), so
  runs can leave it out.
- **Two scenarios per bug where the outcome has two parts.** For the duplicate-Id and invalid-pet bugs, one
  scenario asserts the status code (S6, S8) and the other only what is stored (S7, S9). If the fix returns a
  different error code, the second scenario still turns green.
- **The upper Id boundary is 18 digits, not int64 maximum - 1.** Generated Ids look like the highest Id + 1,
  so a pet near the maximum would make every user's generated Ids overflow and collide (the BUG-002 symptom).

## Test report

Run on 2026-11-16 against `https://petstore.swagger.io/v2`:

| Total | Passed | Failed | Skipped (open bug) | Duration |
|---|---|---|---|---|
| 23 | 9 | 1 (S5: BUG-002 reproduced) | 13 | ~10 s |

Plus 44 Core unit tests, all passing (~0.1 s).

The failed test case and all 13 skipped ones were also run on purpose with the gate off (`BUGS_REPO_PATH`
pointing to a missing folder). Each one failed exactly as its bug describes:

| Bug | Expected | Actual |
|---|---|---|
| [BUG-001](Bugs/BUG-001.md) | `409`, existing pet unchanged | `200`, the existing pet is silently overwritten |
| [BUG-002](Bugs/BUG-002.md) | a distinct generated Id per pet | both pets get `9223372036854775807`, so the second overwrites the first |
| [BUG-003](Bugs/BUG-003.md) | `405 Invalid input`, nothing stored | `200`, a pet without `name`, without `photoUrls` or with `status: flying` is stored |
| [BUG-004](Bugs/BUG-004.md) | `400 Invalid ID supplied` for `GET` and `DELETE` with `abc` or int64 maximum + 1 | `404` with `java.lang.NumberFormatException ...` as the message |
| [BUG-005](Bugs/BUG-005.md) | `DELETE` of a missing pet: `404 Pet not found` | `404` with an empty body |

BUG-002 does not reproduce on every run. It was closed on 2026-11-15 as Cannot reproduce, then reproduced again
on 2026-11-16 and was reopened as intermittent. The cause cannot be confirmed from the outside; the observations
fit an overflow: a generated Id is the highest existing Id + 1, and generated Ids are already close to the int64
maximum, so while any user's pet sits at the maximum, every generated Id overflows to it.

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
  Steps/                       BaseStep, CommonSteps, PetSteps (regions: creation, retrieval, deletion, checks)
  Hooks/                       BugHooks, CleanupHooks (after scenario)
  Tests/Features/              PetStore_Pet.feature
WhereIsMyAi.RestSharpBase.Core.UnitTests/   unit tests per Core folder (FakeHttpHandler)
Bugs/                          local bug repository: one BUG-xxx.md per bug
```

Dependencies point one way only: `Tests -> Api -> Core`.

## AI assistance

`WhereIsMyAi.RestSharpBase.Core` started from my own RestSharp template. The rest (code, scenarios and
documentation) was built with the help of an AI coding assistant (Claude Code), under my direction and review.
Every scenario and every bug report was run and verified against the live API.

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
