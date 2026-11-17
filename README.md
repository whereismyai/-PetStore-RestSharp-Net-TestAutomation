# PetStore Test Automation

API tests for the **Pet** endpoints of the public [Swagger PetStore](https://petstore.swagger.io):
`POST /pet`, `GET /pet/{id}` and `DELETE /pet/{id}`. Scenarios are plain Gherkin, so you can read what each
test checks without reading C#.

Stack: C# / .NET 10, Reqnroll (Gherkin, NUnit), RestSharp, FluentAssertions.

## Coverage

**27 test cases: 13 pass, 14 skipped, 0 fail.** Each skipped test asserts the correct behaviour for one of
6 open API bugs, and runs again once its bug is fixed. The scenarios S1-S17 are listed in
[TEST_RESULTS.md](TEST_RESULTS.md#results-by-scenario).

| Area | Technique | Checked on every run | Expected, skipped while the bug is open | Tests |
|---|---|---|---|---|
| Create, read back, delete | EP, BVA | required fields only; all fields; each `status`; 256-character name; Ukrainian name with emoji; Ids `1` and 18 digits | | S1, S3 |
| Generated Id | EP, BVA | no Id, `0`, `-1` get a positive Id | two such pets get different Ids ([BUG-002](Bugs/BUG-002.md)) | S2, S4 |
| `name` (required) | EP, DT | a value | missing, `null`, `""`, a number ([BUG-003](Bugs/BUG-003.md)) | S1, S7, S9 |
| `photoUrls` (required) | EP, DT | one, several, `[""]` | missing, `null`, `[]` ([BUG-003](Bugs/BUG-003.md)); a string ([BUG-007](Bugs/BUG-007.md)) | S1, S3, S6, S7, S10 |
| Both required fields | DT | both present | both missing ([BUG-003](Bugs/BUG-003.md)) | S1, S7 |
| `status` | EP | `available`, `pending`, `sold`, not sent | `flying`, `Available`, `""` ([BUG-003](Bugs/BUG-003.md)) | S1, S8 |
| `category` | EP | with id; without id (gets id `0`); `{}`; not sent | a string ([BUG-007](Bugs/BUG-007.md)) | S1-S3, S6, S10 |
| `tags` | EP, BVA | several; without id (gets id `0`); `[]`; not sent | `null` ([BUG-003](Bugs/BUG-003.md)) | S1-S3, S6, S9 |
| `id` in the body | EP | explicit; not sent | a decimal ([BUG-003](Bugs/BUG-003.md)); `"abc"` ([BUG-007](Bugs/BUG-007.md)) | S1, S2, S9, S10 |
| Body | EP, error guessing | unknown field ignored; empty -> `405`; broken JSON or XML -> `400` | an array instead of one pet ([BUG-007](Bugs/BUG-007.md)) | S5, S6, S10 |
| Path Id | EP, BVA | existing pet; never created, `0`, `-1` -> `404` | `abc`, `1.5`, int64 max + 1 -> `400` ([BUG-004](Bugs/BUG-004.md)) | S11, S12 |
| Lifecycle | ST | delete -> `404`; delete again -> `404`; reuse a freed Id; delete one of two pets that share tags, the other stays | duplicate Id -> `409`, original kept ([BUG-001](Bugs/BUG-001.md)) | S1, S3, S13, S14 |
| Media types | EP | JSON by default; `Accept: application/xml` on `GET` and `POST` (whole pet as XML); XML body stored; `text/plain` body -> `415` | `Accept: text/plain` -> `406` ([BUG-006](Bugs/BUG-006.md)) | S15-S17 |
| `api_key` header on `DELETE` | EP | sent; not sent | | S1, S17 |

Every `POST` or `DELETE` of a pet is followed by a `GET` that verifies what is stored (the whole pet, or `404`).
EP = equivalence partitioning, BVA = boundary values, DT = decision table, ST = state transitions
([TEST_APPROACH.md](TEST_APPROACH.md)).

Not covered: `PUT /pet`, `GET /pet/findByStatus`, authorization (not in the requirements), body Ids beyond int64.

## Run

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) and access to `https://petstore.swagger.io`.

```bash
git clone https://github.com/whereismyai/PetStore-RestSharp-Net-TestAutomation.git
cd ./PetStore-RestSharp-Net-TestAutomation
cp .env.example .env        # BASE_URL=https://petstore.swagger.io/v2, BUGS_REPO_PATH=Bugs
dotnet test                 # builds, runs the Core unit tests and all scenarios
```

`.env` holds `BASE_URL` (required) and `BUGS_REPO_PATH` (optional; without it, bug-linked tests run instead of
being skipped). An environment variable with the same name wins over the file.

```bash
dotnet test --filter "TestCategory=smoke"                     # 4 core cases, ~5 s
dotnet test --filter "TestCategory=validation"                # one feature: e2e, validation, lifecycle, http
dotnet test --filter "TestCategory!~bug"                      # leave out bug-linked tests
BUGS_REPO_PATH=none dotnet test --filter "TestCategory~bug"   # run bug-linked tests: are the bugs fixed?
dotnet test --filter "TestCategory!=shared-ids"               # leave out Id 1, which other users share
```

## Where to look

| What | Where |
|---|---|
| The latest run, every scenario, what each check is based on, the bugs | [TEST_RESULTS.md](TEST_RESULTS.md) |
| How the tests are designed and why | [TEST_APPROACH.md](TEST_APPROACH.md) |
| The scenarios themselves | [`Tests/Features/`](WhereIsMyAi.PetStore.TestAutomation/Tests/Features): `_E2E` (valid pets), `_Validation` (accepted and rejected input), `_Lifecycle` (state transitions), `_Http` (headers, media types) |
| Bug reports | [`Bugs/`](Bugs) |

## CI

`.github/workflows/ci.yml` runs on every pull request into `main`:

- **`build`** (required): builds the solution and runs the Core unit tests, no network.
- **`test`** (not required): runs the scenarios against the shared public server, which the PR does not control.
- Skipped tests show up as warnings with the bug's id and title, plus a grey `Skipped tests` check.

## Solution layout

| Project | Contents |
|---|---|
| `WhereIsMyAi.RestSharpBase.Core` | Reusable base, no PetStore or test-framework code: RestSharp client wrapper, logger, `.env` and JSON helpers, a small local TMS (`Bugs/*.md` -> skip decision) |
| `WhereIsMyAi.PetStore.TestAutomation` | Pet API client and DTOs, steps, hooks (bug skip, cleanup), feature files |
| `WhereIsMyAi.RestSharpBase.Core.UnitTests` | Unit tests for Core, no network; also checks every `Bugs/*.md` has a valid status |

Dependencies point one way: `Tests -> Api -> Core`.

## AI assistance

`WhereIsMyAi.RestSharpBase.Core` started from my own RestSharp template. I built the tests and their
infrastructure with an AI coding assistant (Claude Code). I made every strategic decision (test strategy,
architecture, coverage scope, how known bugs are handled), reviewed every line, and I am accountable for all of
it. Every scenario and bug report was run and verified against the live API.

The Core unit tests go beyond the 2-hour task: they were created entirely with AI assistance, as a smoke check
of the reusable Core, so the required CI check runs something deterministic. The PetStore scenarios are the
deliverable.

## Next steps

- **CI**: on-demand and nightly runs; publish TRX and an HTML report (e.g. Allure) per run.
- **More Pet operations**: `PUT /pet`, `GET /pet/findByStatus`.
- **More endpoints**: Store and User, each a new client, DTO, steps class and feature file.
