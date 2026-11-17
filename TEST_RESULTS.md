# Test Results

Run on 2026-11-17 against `https://petstore.swagger.io/v2`. Coverage by area: [README.md](README.md#coverage).
How the tests are designed: [TEST_APPROACH.md](TEST_APPROACH.md).

| Test cases | Passed | Failed | Skipped (open bug) | Duration |
|---|---|---|---|---|
| 27 | 13 | 0 | 14 | ~20 s |

Plus 44 Core unit tests, all passing. Smoke suite (`@smoke`): 4 test cases, all passing.

A skipped test is not a gap: it asserts the correct behaviour for an open bug and runs again once the bug is
fixed. With the skip turned off (`BUGS_REPO_PATH=none`), 13 of the 14 fail exactly as their bug describes; the
BUG-002 test passed on this run, because that bug does not reproduce every time.

## Results by scenario

An outline runs once per `Examples` row, so 17 scenarios give 27 test cases.

| # | Feature | Scenario | Cases | Result |
|---|---|---|---|---|
| S1 | e2e, smoke (first row) | A pet can be created, read back and deleted | 5 | Pass |
| S2 | e2e, smoke | A pet created without an Id, with Id 0 or -1 gets a generated Id; a category or tag without an id gets id 0 | 1 | Pass |
| S3 | e2e | A pet with all fields is stored as sent; two pets can share tags and photo URLs, and deleting one leaves the other unchanged | 1 | Pass |
| S4 | e2e | Pets created without a positive Id get different generated Ids | 1 | Skipped: [BUG-002](Bugs/BUG-002.md) |
| S5 | validation, smoke | An empty body, broken JSON or broken XML is rejected and nothing is stored | 1 | Pass |
| S6 | validation | A pet with an empty category object, an empty photo URL and an empty tags list is stored, and an unknown field is ignored | 1 | Pass |
| S7 | validation | A pet with a missing, null or empty required field is rejected with 405 Invalid input and not stored | 7 | Skipped: [BUG-003](Bugs/BUG-003.md) |
| S8 | validation | A pet with an invalid status is rejected with 405 Invalid input and not stored | 1 | Skipped: [BUG-003](Bugs/BUG-003.md) |
| S9 | validation | A pet with a number for a string, a decimal Id or null tags is rejected with 405 Invalid input and not stored | 1 | Skipped: [BUG-003](Bugs/BUG-003.md) |
| S10 | validation | A pet with a value of the wrong JSON type is rejected with 405 Invalid input and not stored | 1 | Skipped: [BUG-007](Bugs/BUG-007.md) |
| S11 | validation | Getting or deleting a pet by an Id that is not an int64 (not a number, a decimal, too large) is rejected with 400 | 1 | Skipped: [BUG-004](Bugs/BUG-004.md) |
| S12 | lifecycle, smoke | A pet that does not exist cannot be retrieved or deleted | 1 | Pass |
| S13 | lifecycle | A deleted pet cannot be deleted again, and its Id can be used for a new pet | 1 | Pass |
| S14 | lifecycle | Creating a pet with an existing Id is rejected with 409 Conflict and keeps the existing pet | 1 | Skipped: [BUG-001](Bugs/BUG-001.md) |
| S15 | http | Replies are JSON by default and XML when the Accept header asks for it, until the pet is deleted | 1 | Pass |
| S16 | http | Getting a pet in a media type the API does not produce is rejected with 406 Not Acceptable | 1 | Skipped: [BUG-006](Bugs/BUG-006.md) |
| S17 | http | A pet is accepted as XML with an XML reply when Accept asks for it, rejected as plain text, and deleted with the api_key header | 1 | Pass |

## What the expected results are based on

| Basis | Expected results |
|---|---|
| **Contract**: `swagger.json` says so | `GET` returns the stored `Pet`, or `404` when there is none; `405` for an empty body or an invalid pet (required fields, `status` enum, field types, no `null` in Swagger 2.0); `400` for a path Id that is not an int64; JSON and XML in and out (`consumes`, `produces`); `{}` for `category` and `""` as a photo URL are valid |
| **Observation**: the spec says nothing, the demo server does it | `200` for `POST` and `DELETE` (the spec defines no success response); the `POST` reply is the pet; a generated Id for no Id, `0` or `-1`; defaults (`[]` for lists not sent, id `0` for a category or tag without one); the messages `Pet not found` and, after `DELETE`, the Id; `400` for broken JSON or XML; `415` for a `text/plain` body; JSON by default; unknown fields ignored; the XML element order |
| **Assumption**: to be agreed with the API owners | an empty `name` or `photoUrls` is rejected; a rejected pet is not stored; a duplicate Id gets `409` (BUG-001); two generated Ids differ (BUG-002); an unsupported `Accept` gets `406` (BUG-006) |

The spec gives error responses no body, so error checks assert the status code only.

## API bugs found

| Bug | Expected | Actual |
|---|---|---|
| [BUG-001](Bugs/BUG-001.md) | a duplicate Id gets `409`, the existing pet is kept (assumption) | `200`, the existing pet is overwritten |
| [BUG-002](Bugs/BUG-002.md) | each pet without an Id gets its own Id | on some runs every such pet gets `9223372036854775807` and overwrites the previous one |
| [BUG-003](Bugs/BUG-003.md) | an invalid pet gets `405 Invalid input` and is not stored | `200`, stored: missing, `null` or empty `name` / `photoUrls`, an invalid `status`, `"name": 123`, a decimal Id, `"tags": null` |
| [BUG-004](Bugs/BUG-004.md) | `GET` / `DELETE` with `abc`, `1.5` or int64 max + 1 get `400` | `404` with `java.lang.NumberFormatException` as the message |
| [BUG-006](Bugs/BUG-006.md) | `Accept: text/plain` gets `406` (assumption) | `500` with an HTML page and a Java stack trace |
| [BUG-007](Bugs/BUG-007.md) | a value of the wrong JSON type (`photoUrls` or `category` as a string, `"id": "abc"`) or an array body gets `405` | `500 something bad happened`; nothing stored |

[BUG-005](Bugs/BUG-005.md) (`DELETE` of a missing pet returns an empty body) was closed as not a bug: the spec
defines no body for that `404`.

BUG-002 reproduced on 2026-11-13, 11-14 and 11-16, not on 11-15. It stays `Open` and its test stays skipped, so
an intermittent server defect does not turn the run red.

## Do the checks catch wrong behaviour?

On 2026-11-16 the negative and status checks were also run against a local mock of the API, once with correct
answers and then with one fault at a time: an invalid pet accepted and stored under another Id, `500` instead of
`405`, field names in upper case. Each fault failed a test; the correct mock passed.
