# Test Results

What the suite verifies on the current build, and which API bugs it found. For how the tests are designed,
see [TEST_APPROACH.md](TEST_APPROACH.md). For how to run them, see [README.md](README.md).

## Summary

Run on 2026-11-17 against `https://petstore.swagger.io/v2`:

| Total | Passed | Failed | Skipped (open bug) | Duration |
|---|---|---|---|---|
| 47 | 28 | 0 | 19 | ~26 s |

Plus 44 Core unit tests, all passing (~0.1 s).

- **Green run.** Every test that can pass on today's API passes.
- **19 skipped test cases are not gaps.** Each one asserts the correct behaviour for one of 6 open API bugs and
  stays skipped until that bug is fixed. A skipped test means "not verified on the current build", and its skip
  reason names the bug.

## Results by scenario

A scenario outline runs once per `Examples` row, so 28 scenarios give 47 test cases. The scenarios are split
into four feature files, one category each (see [TEST_APPROACH.md](TEST_APPROACH.md#four-feature-files-one-question-each)).

| # | Category | Scenario | Test cases | Result |
|---|---|---|---|---|
| S1 | e2e | A pet can be created with the required fields only | 1 | Pass |
| S2 | e2e | A pet can be created with all fields | 1 | Pass |
| S3 | e2e | A pet can be created with each status value | 3 | Pass |
| S4 | e2e | A pet created without a positive Id gets a generated Id | 3 | Pass |
| S5 | e2e | Tags and photo URLs can be shared by two pets and stay after one pet is deleted | 1 | Pass |
| S6 | e2e | Pets created without an Id or with Id 0 get different generated Ids | 1 | Skipped: [BUG-002](Bugs/BUG-002.md) |
| S7 | validation | A pet can be created with a boundary Id | 2 | Pass |
| S8 | validation | A pet can be created with a name of any length | 3 | Pass |
| S9 | validation | A pet can be created with an empty list | 2 | Pass |
| S10 | validation | An invalid pet is rejected with 405 Invalid input and not stored | 6 | Skipped: [BUG-003](Bugs/BUG-003.md) |
| S11 | validation | A pet with a field of the wrong JSON type is rejected with 405 Invalid input and not stored | 3 | Skipped: [BUG-007](Bugs/BUG-007.md) |
| S12 | validation | A body that is not a pet is rejected with 405 Invalid input | 2 | Skipped: [BUG-007](Bugs/BUG-007.md) |
| S13 | validation | A pet without data is rejected with 405 Invalid input | 1 | Pass |
| S14 | validation | A pet in broken JSON is rejected with 400 | 1 | Pass |
| S15 | validation | Getting a pet by an invalid Id is rejected with 400 | 2 | Skipped: [BUG-004](Bugs/BUG-004.md) |
| S16 | validation | Deleting a pet by an invalid Id is rejected with 400 | 2 | Skipped: [BUG-004](Bugs/BUG-004.md) |
| S17 | lifecycle | A deleted pet can no longer be retrieved or deleted | 1 | Pass |
| S18 | lifecycle | A pet can be created again with the Id of a deleted pet | 1 | Pass |
| S19 | lifecycle | Getting a pet that was never created returns 404 Pet not found | 1 | Pass |
| S20 | lifecycle | Deleting a pet that was never created returns 404 | 1 | Pass |
| S21 | lifecycle | Getting and deleting a pet by an Id no pet can have returns 404 | 2 | Pass |
| S22 | lifecycle | Creating a pet with an existing Id is rejected with 409 Conflict | 1 | Skipped: [BUG-001](Bugs/BUG-001.md) |
| S23 | lifecycle | Creating a pet with an existing Id keeps the existing pet | 1 | Skipped: [BUG-001](Bugs/BUG-001.md) |
| S24 | http | Replies are JSON by default and XML when the Accept header asks for it, until the pet is deleted | 1 | Pass |
| S25 | http | Getting a pet in a media type the API does not produce is rejected with 406 Not Acceptable | 1 | Skipped: [BUG-006](Bugs/BUG-006.md) |
| S26 | http | A pet sent as XML is stored like one sent as JSON | 1 | Pass |
| S27 | http | A pet sent in a media type the API does not consume is rejected with 415 and not stored | 1 | Pass |
| S28 | http | A pet can be deleted with the api_key header | 1 | Pass |
| | | **Total** | **47** | **28 pass, 19 skipped** |

## Coverage matrix

The test conditions come from the ISTQB black-box techniques (see
[TEST_APPROACH.md](TEST_APPROACH.md#granular-one-test-condition-per-test-case)). Each row is one equivalence
partition (EP), boundary value (BVA), decision-table rule (DT) or state transition (ST), covered by its own test
case. **Pass** means it is verified on every run. **Blocked** means a test case covers it, but it is skipped while
its bug is open.

| Input | Technique | Partition, boundary, rule or transition | Covered by | Status |
|---|---|---|---|---|
| Body | DT | `name` and `photoUrls` present (required fields only) | S1 | Pass |
| Body | DT | `name` missing / `photoUrls` missing / both missing | S10 | Blocked: BUG-003 |
| Body | EP | all fields | S2 | Pass |
| Body | EP | empty (no data) -> `405` | S13 | Pass |
| Body | EP | broken JSON -> `400` (observation) | S14 | Pass |
| Body | EP | a JSON array instead of one pet | S12 | Blocked: BUG-007 |
| `id` (body) | EP | explicit, new | S1-S3, S5 | Pass |
| `id` (body) | EP | not sent | S4 | Pass |
| `id` (body) | BVA | `-1`, `0` (the server generates an Id: observation), `1` (stored as sent) | S4, S7 | Pass |
| `id` (body) | BVA | 18 digits (above 2^53) | S7 | Pass |
| `id` (body) | EP | not sent / `0` twice: each pet gets a distinct Id | S6 | Blocked: BUG-002 |
| `id` (body) | EP | not a number (`"abc"`) | S12 | Blocked: BUG-007 |
| `name` | EP | English, Ukrainian, emoji, digits and brackets | S2, S3, S5 | Pass |
| `name` | BVA | empty, 1 character, 256 characters (the spec sets no length limit) | S8 | Pass |
| `status` | EP | `available`, `pending`, `sold` | S3 | Pass |
| `status` | EP | not sent | S1, S4, S7-S9 | Pass |
| `status` | EP | not in the enum / wrong case (`Available`) / empty | S10 | Blocked: BUG-003 |
| `category` | EP | with id / not sent | S2 / S1, S3 | Pass |
| `category` | EP | name only (id comes back as `0`) | S6 | Blocked: BUG-002 |
| `category` | EP | a string instead of an object | S11 | Blocked: BUG-007 |
| `photoUrls` | EP | one / several / shared by two pets | S1 / S2 / S5 | Pass |
| `photoUrls` | BVA | empty list `[]` | S9 | Pass |
| `photoUrls` | EP | a string instead of a list | S11 | Blocked: BUG-007 |
| `tags` | EP | several with ids; Ukrainian, Spanish, French and `& " % ,` | S2, S5 | Pass |
| `tags` | EP | not sent / empty list `[]` | S1 / S9 | Pass |
| `tags` | EP | a string instead of a list | S11 | Blocked: BUG-007 |
| `id` (path) | EP | existing pet | S1-S5, S7-S9, S18 | Pass |
| `id` (path) | EP | never created: `GET` -> `404 Pet not found` / `DELETE` -> `404` | S19 / S20 | Pass |
| `id` (path) | BVA | `0`, `-1`: `GET` and `DELETE` -> `404` | S21 | Pass |
| `id` (path) | EP | non-numeric: `GET` / `DELETE` | S15 / S16 | Blocked: BUG-004 |
| `id` (path) | BVA | int64 maximum + 1: `GET` / `DELETE` | S15 / S16 | Blocked: BUG-004 |
| Pet lifecycle | ST | exists -> deleted (`DELETE` -> `200`, the Id as message); deleted -> `GET` `404`, `DELETE` `404` | S17 | Pass |
| Pet lifecycle | ST | deleted -> exists again (`POST` with the same Id) | S18 | Pass |
| Pet lifecycle | ST | exists -> `POST` with the same Id -> `409`, existing pet kept | S22, S23 | Blocked: BUG-001 |
| Pet lifecycle | ST | one of two pets that share a tag and photo URL deleted -> the other unchanged | S5 | Pass |
| Response `Content-Type` | EP | `application/json` (default `Accept`) on `POST`, `GET` and `DELETE` `200` and `GET` `404` | S24 | Pass |
| `Accept` | EP | `application/xml` -> `200`, `Content-Type: application/xml` | S24 | Pass |
| `Accept` | EP | a type the API does not produce (`text/plain`) -> `406` | S25 | Blocked: BUG-006 |
| Request `Content-Type` | EP | `application/xml` -> stored like JSON | S26 | Pass |
| Request `Content-Type` | EP | a type the API does not consume (`text/plain`) -> `415`, not stored | S27 | Pass |
| `api_key` header (`DELETE`) | EP | sent / not sent | S28 / S17 | Pass |

In total: 41 rows: 28 pass, 13 blocked by open bugs. What is deliberately not covered is listed in
[TEST_APPROACH.md](TEST_APPROACH.md#scope).

## What each step checks

Every expected result has a basis (see [TEST_APPROACH.md](TEST_APPROACH.md#why-we-expect-what-we-expect)):
**contract** - `swagger.json` says so; **observation** - the spec says nothing, the demo server does it today;
**assumption** - the spec says nothing, we think the API should do it, to be agreed with the API owners.

| Step | Expected | Basis | Scenarios |
|---|---|---|---|
| `Given the pets with the following Ids do not exist` | `GET` -> `404`; a leftover pet (`200`) is deleted, then `GET` -> `404` | contract (`GET` `200` / `404`) | all with a valid Id |
| `When a pet is created ...` + `status code` | `200` for a valid pet | observation: the spec declares only `405` for `POST /pet` | S1-S9, S17, S18, S22-S26, S28 |
| `the returned pet should be exactly`, after `POST` | the reply is the pet as sent | observation: the spec gives `POST` no response body; field names, required `name` and `photoUrls`: contract (`Pet`) | S1-S4, S6, S7 |
| `the returned pet should be exactly`, after `GET` | `200` and the stored pet equals what was sent | contract (`GET` `200` returns a `Pet`) | S1-S9, S18, S26 |
| Server defaults in an expected pet | `photoUrls` and `tags` are `[]` when not sent; a category or tag sent without an id gets id `0` | observation | S1, S3, S4, S6-S9, S18, S26 |
| `... with a generated Id`, `... with the Id of the created pet named '<name>'` | a pet sent without an Id, with `0` or with `-1` gets a positive Id and can be read back by it | observation: the spec does not say how Ids are assigned | S4, S6 |
| `the created pets should have different Ids` | two pets without an Id get two Ids | assumption: one Id, one pet | S6 |
| `POST` of an empty body + `status code` | `405` | contract (`405 Invalid input`) | S13 |
| `POST` of broken JSON + `status code` | `400` | observation: the spec declares only `405`; `400` is the HTTP code for a malformed request | S14 |
| `POST` of an invalid pet + `status code` | `405` | contract (`405 Invalid input`; `name` and `photoUrls` required, `status` enum, `Pet` field types) | S10-S12 |
| `GET` of a rejected pet + `status code` | `404` | assumption: a rejected pet is not stored | S10, S11, S27 |
| `When ... is deleted via DELETE /pet/{id}` + `status code` | `200` for an existing pet, with or without `api_key` | observation: the spec declares only `400` and `404`; `api_key` is an optional header (contract) | S5, S17, S18, S24, S28 |
| `the response message should be '<id>'`, after `DELETE` | the deleted pet's Id | observation | S17 |
| `GET` or `DELETE` of a pet that is not there (deleted, never created, `0`, `-1`) + `status code` | `404` | contract (`404 Pet not found`) | S17, S19-S21, S24, S28 |
| `the response message should be 'Pet not found'` | exactly `Pet not found` | observation: the `404` has no schema | S17, S19, S21 |
| Second `POST` with the same Id after a `DELETE` | `200`, the new pet is stored | observation: the Id is free again | S18 |
| Second `POST` with the same Id + `status code` | `409` | assumption | S22 |
| `GET` after a second `POST` with the same Id | the original pet | assumption: a create does not overwrite | S23 |
| `GET` / `DELETE` by an invalid Id + `status code` | `400` | contract (`400 Invalid ID supplied`) | S15, S16 |
| `the response Content-Type should be 'application/json'` | `application/json`, any `charset` | contract (`produces`); JSON rather than XML for the client's default `Accept` (JSON listed first): observation | S24 |
| `... retrieved with Accept 'application/xml'` + `Content-Type` | `200`, `application/xml` | contract (`produces`) | S24 |
| `... retrieved with Accept 'text/plain'` + `status code` | `406` | assumption: RFC 9110 allows `406` or ignoring `Accept` | S25 |
| `POST` with `Content-Type: application/xml` | `200`, stored like JSON | contract (`consumes`) | S26 |
| `POST` with `Content-Type: text/plain` + `status code` | `415` | contract (`consumes` lists JSON and XML only); `415` itself: observation, the HTTP code for an unsupported media type | S27 |

## Do the checks catch wrong behaviour?

A passing test only matters if it fails when the API is wrong. On 2026-11-16, the status-value and invalid-pet
scenarios (S3 and S10 today; S10 had 3 cases then) were run against a local mock of the API (not part of this
repository) that answers correctly, and then with one fault at a time:

| Mock behaviour | S3 (3 cases) | S10 (3 cases) |
|---|---|---|
| Correct: invalid pets rejected with `405`, field names as in the spec | Pass | Pass |
| Invalid pet accepted with `200` and stored under another Id; `GET` by the sent Id returns `404` | Pass | **Fail**: expected `405`, got `200` |
| `POST` of an invalid pet returns `500` | Pass | **Fail**: expected `405`, got `500` |
| Field names in upper case (`ID`, `NAME`, `PHOTOURLS`) | **Fail**: `'id' must be in the response` | Pass |

Before these fixes, the "not stored" check passed in the second and third rows, and S3 passed in the last one.

## API bugs found

All skipped test cases were also run on purpose with the bug gate off (`BUGS_REPO_PATH` pointing to a missing
folder), to confirm that each bug is still there:

| Bug | Expected | Actual |
|---|---|---|
| [BUG-001](Bugs/BUG-001.md) | `409`, existing pet unchanged (assumption, to be agreed) | `200`, the existing pet is silently overwritten |
| [BUG-002](Bugs/BUG-002.md) | a distinct generated Id per pet | on some runs, both pets get `9223372036854775807`, so the second overwrites the first |
| [BUG-003](Bugs/BUG-003.md) | `405 Invalid input`; nothing stored (assumption) | `200`, a pet without `name`, without `photoUrls`, without both, or with `status` `flying`, `Available` or empty is stored |
| [BUG-004](Bugs/BUG-004.md) | `400` for `GET` and `DELETE` with `abc` or int64 maximum + 1 | `404` with `java.lang.NumberFormatException ...` as the message |
| [BUG-006](Bugs/BUG-006.md) | `406` for `GET` with `Accept: text/plain` (assumption, to be agreed) | `500` with an HTML page and a Java stack trace |
| [BUG-007](Bugs/BUG-007.md) | `405 Invalid input` for a field of the wrong JSON type or an array body; nothing stored (assumption) | `500`, `something bad happened`; nothing is stored |

On 2026-11-17, 18 of the 19 test cases failed exactly as their bug describes. S6 (BUG-002) passed on that run:
the bug does not reproduce every time.

[BUG-005](Bugs/BUG-005.md) (`DELETE` of a missing pet returns `404` with an empty body) was closed as **not a
bug**: the spec gives that response no schema, so no body is expected. Its scenario was removed.

BUG-002 reproduced on 2026-11-13, 11-14 and 11-16, but not on 11-15. The server is a black box to us and is
shared with other users, so the cause cannot be confirmed from the outside. The observations fit an overflow: a
generated Id is the highest existing Id + 1, and other users' pets already fill the Ids just below the int64
maximum. The bug stays `Open` and S6 stays skipped, so an intermittent server defect does not turn the run red.
