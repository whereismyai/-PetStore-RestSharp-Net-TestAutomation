# Test Approach

How the suite is designed and why. What it covers: [README.md](README.md#coverage). The latest run:
[TEST_RESULTS.md](TEST_RESULTS.md).

## Method

We see the API only from the outside, so the test conditions come from `swagger.json` and the ISTQB
black-box techniques (CTFL v4.0, 4.2):

| Technique | Applied to | Example |
|---|---|---|
| Equivalence partitioning (EP) | every field, the body, path Ids, media types, headers | `status`: each enum value, not sent, not in the enum, wrong case, empty |
| Boundary values (BVA) | Ids, name length, list length | body Id `-1` / `0` / `1`; `tags: []` |
| Decision table (DT) | the required fields `name` and `photoUrls` | each one missing, `null`, empty or with a value; both missing |
| State transitions (ST) | the pet lifecycle: does not exist, exists, deleted | delete, then `GET` and `DELETE` again; reuse the freed Id; duplicate Id |
| Error guessing | typical client mistakes | empty body, broken JSON or XML, wrong JSON types, wrong `Content-Type` |

Exhaustive partitions belong in the backend's own unit tests, which we cannot reach. Here we keep the number of
end-to-end test cases small.

## Rules

- **Valid inputs are combined.** One `Examples` row carries several valid partitions (a status, a kind of
  name, an Id boundary), so each partition appears in at least one row.
- **One fault per negative case.** Every pet is sent with its required fields unless that field is under test.
  Each missing, `null` or empty required field is its own test case. Other invalid inputs for the same bug run
  one after another in one scenario.
- **Data, not just status.** Every `POST` or `DELETE` of a pet is followed by a `GET` that checks what is
  stored: the whole pet, or `404` for a deleted or rejected one.
- **The expected pet is written in full** in the feature file and compared field by field, list order included.
  It is never copied from an earlier response, because that response could be wrong too.
- **Every expected result has a basis**: contract (`swagger.json`), observation (the spec says nothing, the
  server does it today) or assumption (to be agreed with the API owners). The list is in
  [TEST_RESULTS.md](TEST_RESULTS.md#what-the-expected-results-are-based-on).
- **A scenario's name says what it checks.** A new check either goes into a scenario whose name already covers
  it, or the name is extended.

## Feature files

| File | Question |
|---|---|
| `PetStore_Pet_E2E.feature` (`@e2e`) | Is a valid pet stored as sent? |
| `PetStore_Pet_Validation.feature` (`@validation`) | Which inputs are accepted, which rejected? |
| `PetStore_Pet_Lifecycle.feature` (`@lifecycle`) | Are the state transitions right? |
| `PetStore_Pet_Http.feature` (`@http`) | Are media types and headers right? |

`@smoke` marks the core cases (create, read, delete; generated Id; empty or broken body; a pet that does not
exist). They never carry a bug, so a red smoke run means a real outage. Guidelines: a feature file stays around
200 lines and a scenario around 16 steps.

## Known bugs

A test for a known bug asserts the **expected** behaviour, has exactly one tag `@bug:<id>`, and never shares a
scenario with checks that pass today. The bug is a file `Bugs/<id>.md`. Before the scenario starts, `BugHooks`
reads its `Status:`:

- `Open`: the test is skipped, and the skip reason names the bug.
- `Fixed` or `Closed`: the test runs, with no code change.
- Anything else: the test runs with a warning, and a Core unit test fails the build.

An intermittent bug (BUG-002) stays `Open`, so the run stays green. To check whether the bugs are fixed, run
`BUGS_REPO_PATH=none dotnet test --filter "TestCategory~bug"`. A bug that stops reproducing becomes `Closed` with
`Resolution: Cannot reproduce`; one the contract does not support becomes `Closed` with `Resolution: Not a bug`.
In a real project the hook would read the ticket status from Jira instead of `Bugs/*.md`.

## Safe on a shared server

- **Clean start.** `Given the pets with the following Ids do not exist` checks each Id the scenario uses and
  deletes a leftover pet first.
- **No collisions.** Explicit Ids carry a random 4-digit prefix per scenario (`{run_id_int}12345` ->
  `482112345`), which is logged, so a failed run's data can be found.
- **Always cleans up.** Every Id a scenario sent or got back is deleted after it, whether it passed or failed.
  Cleanup never fails a test; it only logs a warning.

## Scope

Covered: `POST /pet`, `GET /pet/{id}`, `DELETE /pet/{id}`, the request and response `Content-Type`, `Accept` on
`GET` and `POST`, and `api_key` on `DELETE`.

Not covered: `PUT /pet` and `GET /pet/findByStatus`; authorization (not in the requirements); body Ids beyond
int64 (the client sends a `long`); response headers the spec does not define (`Date`, `Server`, CORS). Some
partitions have one test standing for others: `photoUrls` as a string also stands for `tags` as a string,
`"name": 123` for `"photoUrls": [1]`, `"tags": null` for `"category": null`.

## Trade-offs

- **Combined test cases.** 27 test cases stand for many more conditions. When one fails, the failed step and
  its logged request show which condition broke.
- **A sequential negative scenario stops at its first wrong answer.** The later inputs are checked once the
  first one is fixed.
- **Skipped, not failed.** Tests for open bugs keep the run green and leave no data on the server, but a fix is
  noticed only when they are run with the skip turned off.
- **Id `1` is shared.** It cannot take the random prefix, so it may replace another user's pet `1` (deleted
  afterwards). It is tagged `@shared-ids`, so runs can leave it out.
- **The upper Id boundary is 18 digits, not the int64 maximum.** A pet near the maximum would make the server's
  generated Ids overflow for every user (the BUG-002 symptom).
- **Concurrent writes are not handled.** Another user can write the same Id while a scenario runs; there are no
  retries.
