# Test Approach

How the suite is designed and why. For the current results and coverage, see [TEST_RESULTS.md](TEST_RESULTS.md).
For how to run the tests, see [README.md](README.md).

## Few scenarios, every condition covered

Most equivalence classes and boundaries belong in the backend's own unit and integration tests. This project
has no access to them, so it covers them from the outside with as few end-to-end test cases as possible.

The test conditions come from the ISTQB black-box techniques (CTFL v4.0, chapter 4.2), applied to the `Pet`
schema and the three operations in `swagger.json`:

| Technique | Applied to | Example |
|---|---|---|
| Equivalence partitioning | every field, the body, path Ids, request and response media types | `status`: each enum value, not sent, not in the enum, wrong case, empty |
| Boundary value analysis | Ids, name length, list length | body Id `-1` / `0` / `1`; `tags: []` |
| Decision table | the required fields `name` and `photoUrls` | both present, either missing, both missing: four rules; each field also `null` and empty (`""`, `[]`) |
| State transition | the pet lifecycle: does not exist, exists, deleted | delete, then `GET` and `DELETE` again; create again with the freed Id |
| Error guessing | typical client mistakes | empty body, broken JSON, wrong JSON types, wrong `Content-Type` |

- **Valid conditions are combined.** One `Examples` row carries several valid partitions at once (a status, a
  kind of name, an Id boundary), so each one is covered by at least one row (ISTQB "each choice" coverage).
  Every row walks the whole lifecycle: create, read back, delete, read again.
- **Invalid conditions run one after another.** A negative scenario sends its invalid pets in sequence, each
  with exactly one thing wrong, so no request can hide another's fault. The scenario stops at the first wrong
  answer; the remaining requests are checked once that one is fixed.
- **Every status code is followed by the data.** After a `POST` or `DELETE`, a `GET` checks what is really
  stored: the whole pet, or `404` for a deleted or rejected one. Only an invalid path Id has no data to read.
- **Bug-linked checks stay apart.** A scenario that asserts a known bug is skipped while the bug is open, so it
  never carries a check that passes today.
- **Traceable.** The coverage matrix in [TEST_RESULTS.md](TEST_RESULTS.md#coverage-matrix) maps every condition
  to the test case that covers it.

## Four feature files, one question each

Scenarios are grouped by the question they answer, not by HTTP method. Each file has a feature tag, which
becomes an NUnit category, so one group can be run on its own: `dotnet test --filter "TestCategory=http"`.

| File | Tag | Question |
|---|---|---|
| `PetStore_Pet_E2E.feature` | `@e2e` | Is a valid pet stored as sent? Required fields only, all fields, each status, generated Ids |
| `PetStore_Pet_Validation.feature` | `@validation` | Which inputs are accepted and which are rejected? Boundaries, missing fields, wrong types, empty or broken body, invalid path Ids |
| `PetStore_Pet_Lifecycle.feature` | `@lifecycle` | Are the state transitions right? Delete, read or delete a pet that is not there, reuse a freed Id, duplicate Id |
| `PetStore_Pet_Http.feature` | `@http` | Is the HTTP layer right? Response `Content-Type`, `Accept`, request `Content-Type`, the `api_key` header |

All four also carry `@pet`, the endpoint group. A new check goes into the file whose question it answers. A
new endpoint group gets its own files, for example `PetStore_Store_E2E.feature`.

Two size limits keep files and scenarios readable. Both are guidelines against extremes, not hard caps:

- **A feature file stays around 200 lines.** A little over is fine (the longest today, `PetStore_Pet_E2E.feature`,
  has 163); a file that keeps growing is split by question, not by HTTP method.
- **A scenario has at most 16 steps** (`Given`/`When`/`Then`/`And` lines), the longest today (S14). A longer
  scenario needs a reason, for example one pet walked through every kind of reply. Otherwise split it.

## Smoke suite

The core cases are tagged `@smoke`. They form a quick check (~5 s) that the API works at all, before or instead
of the full run: `dotnet test --filter "TestCategory=smoke"`.

| Smoke test case | What it covers |
|---|---|
| S1, required fields only row | `POST`, `GET`, `DELETE`, then `GET` -> `404 Pet not found` |
| S2 | generated Id for a pet sent without an Id or with `0`, read back and deleted |
| S5 | empty body -> `405`, broken JSON -> `400`, nothing stored |
| S11 | `GET` and `DELETE` of a pet that does not exist -> `404` |

A smoke test is never linked to a bug: it must pass on today's API, so a red smoke run means a real outage or
regression. The tag goes on the scenario, or on its own `Examples` block for a single row (S1), so the smoke
suite repeats no scenario.

## Bugs are covered at the level we can reach

When the suite finds a defect, we cover it at the level available to us: the end-to-end API. Each bug gets a
file in `Bugs/` and its own scenario that:

- asserts the **expected** behaviour, not the current broken one;
- carries exactly one tag, `@bug:<id>`;
- is **skipped** while the bug is `Open`, and runs again on its own once the bug is `Fixed` or `Closed`.

That is why some test cases are skipped on every run today. They are not missing or broken tests: they are the
acceptance checks for the open bugs, ready to turn green when the fixes land.

How the skip works: before a bug-linked scenario starts, `BugHooks` reads `Status:` from `Bugs/<id>.md`.

| Status | Result |
|---|---|
| `Open` | Skipped, with `Blocked by open bug <id>: <title>` as the reason |
| `Fixed` or `Closed` | Runs, with no code change |
| Anything else | Runs with a warning; a Core unit test fails the build |

To check whether the bugs are fixed, run them with the gate off:
`BUGS_REPO_PATH=none dotnet test --filter "TestCategory~bug"`.

The local bug repository is a stand-in, added in a single prompt only to show the mechanism. In a real project
the hook would read the status of the blocking ticket from Jira instead of `Bugs/*.md`.

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
   `Status: Closed` with a `Resolution: Cannot reproduce` line. If it comes back, set `Status: Open` again.

## How a scenario checks the API

We can't see inside the API, so a scenario explores it one step at a time, like exploring the "fog of war". It sends a
request, checks the answer, and only then takes the next step.

- **Every step has its own expected result.** If an answer is wrong, the scenario stops right there, and the
  failed step tells you what broke.
- **The next step checks the one before.** A reply can be wrong, so we don't take it on trust:
  - after `POST`, a `GET` reads back what was really stored (S1-S4, S6, S12-S16);
  - after `DELETE`, a `GET` checks the pet is gone (S1-S3, S12, S14, S16);
  - after deleting one of two pets, a `GET` checks the other one is untouched (S3);
  - after a rejected `POST`, a `GET` checks nothing was stored (S5, S7-S9, S16).

### Why we expect what we expect

Every expected result has a **basis**: the reason we expect it. It's about the source of the expectation, not
about which requests a scenario sends.

| Basis | What it means | Example |
|---|---|---|
| **contract** | `swagger.json` says so | `GET` of a deleted pet returns `404` |
| **observation** | The spec says nothing, so we check what the demo server does today | `POST` returns `200` and the pet: the spec only declares `405` |
| **assumption** | The spec says nothing, but we think the API should do it. To agree with the API owners | A duplicate Id is rejected with `409` (BUG-001) |

[TEST_RESULTS.md](TEST_RESULTS.md#what-each-step-checks) gives the basis of every step.

### What else the checks make sure of

- **The whole pet, written out in full.** Every `POST` reply and `GET` read-back is compared with the complete
  expected pet from the feature file: all fields, category and tag ids, list order. Pets with generated Ids
  must match everything except the Id. We never copy the expected pet from an earlier response, because that
  response could be wrong too.
- **Missing fields fail by name, in exact case.** The response DTO is all-nullable and JSON is read
  case-sensitively, so a missing `name` or `photoUrls` (required by the contract) or `id` (every pet is read back
  by it) fails with that field's name. A body with `NAME` instead of `name` fails too. `tags` is not required by
  the contract; when no tags were sent, the expected `[]` is an observed server default.
- **One fault per negative case.** Every pet is sent with its required fields (`name`, `photoUrls`) unless
  that field is under test, so an invalid-input test case has exactly one thing wrong. Inputs a table cannot
  express (an empty list, a wrong JSON type, broken JSON, XML) are sent as a raw body, written in the feature file.
- **Messages only where the server is known to send them.** In the spec, the `400`, `404` and `405`
  responses have a `description` but no `schema`, and in OpenAPI 2.0 no schema means no response body. So
  the bug scenarios check status codes only. The `message` field is compared exactly in two cases, both
  observations of the demo server: `DELETE` of an existing pet returns its Id (S1), and `GET` of a pet that
  is not there returns `Pet not found` (S1, S11).
- **Media type, not other headers.** The only response header the contract defines for these endpoints is
  `Content-Type` (`produces`: JSON or XML), so it is the only one checked, all in S14: `application/json` once
  per kind of reply, `application/xml` when `Accept` asks for it. Only the media type is compared, not the
  `charset`. The data scenarios do not repeat it.
- **Varied test data.** Names in English and Ukrainian, with emoji, digits and brackets; tags add Spanish,
  French and `& " % ,`.

## Safe on a shared server

- **Clean start.** Each scenario begins with `Given the pets with the following Ids do not exist`, a table with
  one row per valid Id it uses. For each Id the step sends `GET /pet/{id}`: `404` means the Id is free. On `200` (a pet left by an earlier run or
  another user), it deletes that pet, checks that `GET` now returns `404`, and logs a warning. Only then does
  the scenario start. Invalid Ids (`abc`, int64 maximum + 1), `0` and negative Ids are not checked: no pet can be
  stored there (the server gives a pet sent with `0` or a negative Id a generated Id). The step also records its
  Ids for cleanup, so a pet stored by a raw-body `POST` is deleted too.
- **No Id collisions.** Explicit Ids are prefixed with `{run_id_int}`, a random 4-digit number per scenario
  (`{run_id_int}12345` becomes `482112345`), in the request, the checks and the `GET`/`DELETE` steps. The
  number is logged when the scenario starts, so you can find a failed run's data on the server.
- **Always cleans up.** Every pet a scenario creates is recorded, both with the Id the reply returned and the
  Id that was sent (in case the reply is wrong or a rejected pet was stored anyway). `CleanupHooks` deletes all
  of them after the scenario, whether it passed or failed.

## Easy to read and debug

- **Readable steps.** Step text names the HTTP method and URL. A pet is addressed by Id, by name or as the
  last created pet.
- **One log per scenario.** Each request is logged as `Post /pet -> 200`, with the request body for `POST`
  and `PUT`, on the test's own thread. Every scenario's report contains exactly its own requests and warnings.
- **Ready for parallel runs, run serially.** All scenario state comes from Reqnroll DI, one instance per
  scenario; nothing is static. Parallel runs are not enabled, but a trial with 6 scenarios at once passed with
  correct per-scenario logs.

## Scope

Covered: `POST /pet`, `GET /pet/{id}` and `DELETE /pet/{id}`, including the request and response
`Content-Type`, the `Accept` header of `GET` and the `api_key` header of `DELETE`. The coverage matrix, with the status of each
class on the current build, is in [TEST_RESULTS.md](TEST_RESULTS.md#coverage-matrix).

Not covered: body Ids beyond int64 (the client sends a `long`), wrong JSON types the server converts instead of
rejecting (`"name": 123` is stored as `"123"`, see [BUG-007](Bugs/BUG-007.md)), `PUT /pet` and
`GET /pet/findByStatus`. Response headers the spec does not define
(`Date`, `Server`, CORS, `Content-Length`) are not asserted: they belong to the server setup, not the API.
Security headers belong to a separate security scan.

## Trade-offs

- **Verbose expected pets.** Each expected pet repeats the request table (three times in S1), but every
  checked value is visible in the feature file.
- **Combined test cases.** 20 test cases (~17 s) cover 34 conditions. A failing test case can stand for any of
  its conditions: the failed step and its request in the log tell you which one.
- **A negative scenario stops at its first wrong answer.** While BUG-003 is open, only the first invalid status
  is checked on a run with the gate off; the others are checked once that one is fixed. Required fields are the
  exception: each missing, `null` or empty field is its own test case (S7), so all of them are checked.
- **Blocked scenarios are skipped, not run as known failures.** No side effects on the shared server and a
  green run, but a fix is only noticed when you run them with the gate off. This also applies to BUG-002, which
  reproduces on some runs only.
- **Leftovers are removed, concurrent writes are not.** The clean-start step frees every valid Id before the
  scenario, at the cost of one extra `GET` per Id. Another user can still write the same Id while the scenario
  runs; there are no retries.
- **Boundary Id `1` is shared.** It cannot take the `{run_id_int}` prefix, so the clean-start step deletes
  another user's pet `1`, if there is one, and the scenario then creates and deletes its own. That is
  acceptable on a public demo server. Only that example is tagged `@shared-ids` (its own `Examples` block), so
  runs can leave it out.
- **"Not stored" is checked only after a rejection.** For a duplicate Id (S13), the `GET` of the original pet
  runs only after the assumed `409`; for an invalid pet (S5, S7-S9), the `GET` by the sent Id runs only after the
  rejection. A `GET` by the sent Id after an accepted or failed `POST` proves nothing, because the pet could be
  stored under another Id.
- **The upper Id boundary is 18 digits, not int64 maximum - 1.** Generated Ids look like the highest Id + 1,
  so a pet near the maximum would make every user's generated Ids overflow and collide (the BUG-002 symptom).
