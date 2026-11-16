# Test Results

What the suite verifies on the current build, and which API bugs it found. For how the tests are designed,
see [TEST_APPROACH.md](TEST_APPROACH.md). For how to run them, see [README.md](README.md).

## Summary

Run on 2026-11-16 against `https://petstore.swagger.io/v2`:

| Total | Passed | Failed | Skipped (open bug) | Duration |
|---|---|---|---|---|
| 19 | 9 | 0 | 10 | ~11 s |

Plus 44 Core unit tests, all passing (~0.1 s).

- **Green run.** Every test that can pass on today's API passes.
- **10 skipped test cases are not gaps.** Each one asserts the correct behaviour for one of 4 open API bugs and
  stays skipped until that bug is fixed. A skipped test means "not verified on the current build", and its skip
  reason names the bug.

## Results by scenario

A scenario outline runs once per `Examples` row, so 10 scenarios give 19 test cases. **Basis** says where the
expected behaviour comes from (see [TEST_APPROACH.md](TEST_APPROACH.md#what-the-checks-verify)).

| # | Scenario | Test cases | Basis | Result |
|---|---|---|---|---|
| S1 | A pet can be created with all fields and deleted only once | 1 | contract; `DELETE` `200`: observation | Pass |
| S2 | A pet can be created and deleted with each status value | 3 | contract; `DELETE` `200` and messages: observation | Pass |
| S3 | A pet can be created and deleted with boundary Ids and name lengths | 4 | contract; `DELETE` `200` and message: observation | Pass |
| S4 | Tags and photo URLs can be shared by two pets and stay after one pet is deleted | 1 | contract; `DELETE` `200`: observation | Pass |
| S5 | Pets created without an Id or with Id 0 get their own generated Ids | 1 | assumption: one Id, one pet | Skipped: [BUG-002](Bugs/BUG-002.md) |
| S6 | Creating a pet with an existing Id is rejected with 409 Conflict | 1 | assumption, to be agreed | Skipped: [BUG-001](Bugs/BUG-001.md) |
| S7 | Creating a pet with an existing Id keeps the existing pet | 1 | assumption, to be agreed | Skipped: [BUG-001](Bugs/BUG-001.md) |
| S8 | An invalid pet is rejected with 405 Invalid input and not stored | 3 | contract | Skipped: [BUG-003](Bugs/BUG-003.md) |
| S9 | Getting a pet by an invalid Id is rejected with 400 | 2 | contract | Skipped: [BUG-004](Bugs/BUG-004.md) |
| S10 | Deleting a pet by an invalid Id is rejected with 400 | 2 | contract | Skipped: [BUG-004](Bugs/BUG-004.md) |
| | **Total** | **19** | | **9 pass, 10 skipped** |

## Coverage matrix

Each row is one equivalence class or boundary. **Pass** means it is verified on every run. **Blocked** means a
scenario covers it, but that scenario is skipped while its bug is open, so the class is not verified on the
current build.

| Input | Equivalence class or boundary | Covered by | Status |
|---|---|---|---|
| `id` (body) | explicit, new | S1, S2, S4 | Pass |
| `id` (body) | boundaries: `1` (smallest Id stored as sent), 18 digits (above 2^53) | S3 | Pass |
| `id` (body) | explicit, already used | S6, S7 | Blocked: BUG-001 |
| `id` (body) | not sent / `0` (the server generates a distinct Id) | S5 | Blocked: BUG-002 |
| `id` (path) | existing pet | S1-S4 | Pass |
| `id` (path) | deleted pet: `GET` -> `404` (message `Pet not found`: observation) | S2 | Pass |
| `id` (path) | non-numeric: `GET` / `DELETE` | S9 / S10 | Blocked: BUG-004 |
| `id` (path) | int64 maximum + 1: `GET` / `DELETE` | S9 / S10 | Blocked: BUG-004 |
| `name` | English, Ukrainian, emoji, digits and brackets | S1, S2, S4 | Pass |
| `name` | empty string (accepted: the spec sets no minimum length) | S3 | Pass |
| `name` | 256 characters | S3 | Pass |
| `name` | missing (required) | S8 | Blocked: BUG-003 |
| `status` | `available`, `pending`, `sold` | S2 | Pass |
| `status` | not sent | S3, S4 | Pass |
| `status` | not in the enum | S8 | Blocked: BUG-003 |
| `category` | with id / not sent | S1 / S2-S4 | Pass |
| `category` | name only (id comes back as `0`) | S5 | Blocked: BUG-002 |
| `photoUrls` | one / several / shared by two pets | S2, S3 / S1 / S4 | Pass |
| `photoUrls` | missing (required) | S8 | Blocked: BUG-003 |
| `tags` | several with ids; Ukrainian, Spanish, French and `& " % ,` | S1, S4 | Pass |
| `tags` | not sent / shared by two pets | S2, S3 / S4 | Pass |
| `DELETE` | existing pet -> `200`, Id as message (observation) | S2, S3 (status only: S1, S4) | Pass |
| `DELETE` | already deleted -> `404` | S1 | Pass |
| `DELETE` | one of two pets that share a tag and photo URL | S4 | Pass |

In total: 24 rows: 16 pass, 8 blocked by open bugs. What is deliberately not covered is listed in
[TEST_APPROACH.md](TEST_APPROACH.md#scope).

## Do the checks catch wrong behaviour?

A passing test only matters if it fails when the API is wrong. On 2026-11-16, S2 and S8 were run against a
local mock of the API (not part of this repository) that answers correctly, and then with one fault at a time:

| Mock behaviour | S2 (3 cases) | S8 (3 cases) |
|---|---|---|
| Correct: invalid pets rejected with `405`, field names as in the spec | Pass | Pass |
| Invalid pet accepted with `200` and stored under another Id; `GET` by the sent Id returns `404` | Pass | **Fail**: expected `405`, got `200` |
| `POST` of an invalid pet returns `500` | Pass | **Fail**: expected `405`, got `500` |
| Field names in upper case (`ID`, `NAME`, `PHOTOURLS`) | **Fail**: `'id' must be in the response` | Pass |

Before these fixes, the "not stored" check passed in the second and third rows, and S2 passed in the last one.

## API bugs found

All skipped test cases were also run on purpose with the bug gate off (`BUGS_REPO_PATH` pointing to a missing
folder), to confirm that each bug is still there:

| Bug | Expected | Actual |
|---|---|---|
| [BUG-001](Bugs/BUG-001.md) | `409`, existing pet unchanged (assumption, to be agreed) | `200`, the existing pet is silently overwritten |
| [BUG-002](Bugs/BUG-002.md) | a distinct generated Id per pet | on some runs, both pets get `9223372036854775807`, so the second overwrites the first |
| [BUG-003](Bugs/BUG-003.md) | `405 Invalid input`, nothing stored | `200`, a pet without `name`, without `photoUrls` or with `status: flying` is stored |
| [BUG-004](Bugs/BUG-004.md) | `400` for `GET` and `DELETE` with `abc` or int64 maximum + 1 | `404` with `java.lang.NumberFormatException ...` as the message |

On 2026-11-16, 9 of the 10 test cases failed exactly as their bug describes. S5 (BUG-002) passed on that run:
the bug does not reproduce every time.

[BUG-005](Bugs/BUG-005.md) (`DELETE` of a missing pet returns `404` with an empty body) was closed as **not a
bug**: the spec gives that response no schema, so no body is expected. Its scenario was removed.

BUG-002 reproduced on 2026-11-13, 11-14 and 11-16, but not on 11-15. The server is a black box to us and is
shared with other users, so the cause cannot be confirmed from the outside. The observations fit an overflow: a
generated Id is the highest existing Id + 1, and other users' pets already fill the Ids just below the int64
maximum. The bug stays `Open` and S5 stays skipped, so an intermittent server defect does not turn the run red.
