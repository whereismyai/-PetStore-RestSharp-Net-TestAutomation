# Test Approach

How the suite is designed and why. For the current results and coverage, see [TEST_RESULTS.md](TEST_RESULTS.md).
For how to run the tests, see [README.md](README.md).

## A small, atomic suite

Most equivalence classes and boundaries belong in the backend's own unit and integration tests. This project
has no access to the backend or its tests, so the suite covers them from the outside, with as few end-to-end
scenarios as possible:

- **One behaviour per scenario.** When a scenario fails, its name tells you what broke.
- **No duplicated checks.** Steps inside a scenario build on each other (create, read back, delete, read
  again), but no scenario repeats another one's checks. Every `Examples` row adds a new equivalence class or
  boundary.

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

## What the checks verify

- **Every expectation has a stated basis.** Each check is one of three kinds, and
  [TEST_RESULTS.md](TEST_RESULTS.md#results-by-scenario) names the basis of every scenario:
  - **contract**: written in `swagger.json` (required fields, enums, declared status codes);
  - **assumption**: a reasonable rule the spec does not state, to be agreed with the API owners (BUG-001's
    `409` and "create does not overwrite", BUG-002's distinct generated Ids);
  - **observation**: what the demo server does where the spec says nothing (`DELETE` answers `200`, which the
    spec does not declare, the messages below, `[]` for lists that were not sent).
- **What is stored, not only the reply.** In the scenarios that create pets to check them (S1-S5), every pet's
  `POST` reply and its `GET` read-back are compared with the whole expected pet, written in the feature file.
  With a known Id, both must match it exactly: all fields, category and tag ids, list order. Pets with
  generated Ids must match everything except the Id. The expected pet is never taken from an earlier response,
  because that response could repeat a wrong value.
- **Missing fields fail by name, in exact case.** The response DTO is all-nullable and JSON is read
  case-sensitively, so a missing `name` or `photoUrls` (required by the contract) or `id` (every pet is read back
  by it) fails with that field's name. A body with `NAME` instead of `name` fails too. `tags` is not required by
  the contract; when no tags were sent, the expected `[]` is an observed server default.
- **One fault per negative case.** Every pet is sent with its required fields (`name`, `photoUrls`) unless
  that field is under test, so an invalid-input scenario has exactly one thing wrong.
- **Messages only where the server is known to send them.** In the spec, the `400`, `404` and `405`
  responses have a `description` but no `schema`, and in OpenAPI 2.0 no schema means no response body. So
  the bug scenarios check status codes only. The `message` field is compared exactly in two places, both
  observations of the demo server: `DELETE` of an existing pet returns its Id, and `GET` of a deleted pet
  returns `Pet not found` (S2, S3).
- **Varied test data.** Names in English and Ukrainian, with emoji, digits and brackets; tags add Spanish,
  French and `& " % ,`.

## Safe on a shared server

- **Clean start.** Each scenario begins with `Given the pets with the following Ids do not exist`, a table with
  one row per valid Id it uses. For each Id the step sends `GET /pet/{id}`: `404` means the Id is free. On `200` (a pet left by an earlier run or
  another user), it deletes that pet, checks that `GET` now returns `404`, and logs a warning. Only then does
  the scenario start. Invalid Ids (`abc`, int64 maximum + 1) and `0` are not checked: no pet can be stored there.
- **No Id collisions.** Explicit Ids are prefixed with `{run_id_int}`, a random 4-digit number per scenario
  (`{run_id_int}12345` becomes `482112345`), in the request, the checks and the `GET`/`DELETE` steps. The
  number is logged when the scenario starts, so you can find a failed run's data on the server.
- **Always cleans up.** Every pet a scenario creates is recorded, both with the Id the reply returned and the
  Id that was sent (in case the reply is wrong or a rejected pet was stored anyway). `CleanupHooks` deletes all
  of them after the scenario, whether it passed or failed.
- **No pets created only to be deleted.** Delete checks are part of scenarios that already create pets, and
  those scenario names say so.

## Easy to read and debug

- **Readable steps.** Step text names the HTTP method and URL. A pet is addressed by Id, by name or as the
  last created pet.
- **One log per scenario.** Each request is logged as `Post /pet -> 200`, with the request body for `POST`
  and `PUT`, on the test's own thread. Every scenario's report contains exactly its own requests and warnings.
- **Ready for parallel runs, run serially.** All scenario state comes from Reqnroll DI, one instance per
  scenario; nothing is static. Parallel runs are not enabled, but a trial with 6 scenarios at once passed with
  correct per-scenario logs.

## Scope

Covered: `POST /pet`, `GET /pet/{id}` and `DELETE /pet/{id}`. The coverage matrix, with the status of each
class on the current build, is in [TEST_RESULTS.md](TEST_RESULTS.md#coverage-matrix).

Not covered: negative Ids (the API treats them like `0` and generates an Id; the spec defines no rule for
them), body Ids beyond int64 (the client sends a `long`), an empty `photoUrls` list, the `api_key` header on
`DELETE`, malformed JSON, `PUT /pet` and `GET /pet/findByStatus`.

## Trade-offs

- **Verbose expected pets.** Each expected pet repeats the request table (three times in S1), but every
  checked value is visible in the feature file.
- **Delete checks inside creation scenarios.** Fewer pets on the shared server, but a failed delete also
  fails that scenario.
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
- **Duplicate Id: two scenarios; invalid pet: one.** For a duplicate Id, one scenario asserts the assumed
  `409` (S6) and the other only that the existing pet is kept (S7), so S7 turns green with any error code. For
  an invalid pet (S8), "not stored" is checked only after the `405`. A `GET` by the sent Id after an accepted
  or failed `POST` proves nothing, because the pet could be stored under another Id.
- **The upper Id boundary is 18 digits, not int64 maximum - 1.** Generated Ids look like the highest Id + 1,
  so a pet near the maximum would make every user's generated Ids overflow and collide (the BUG-002 symptom).
