Feature: Pet endpoints
    A pet that the API accepts must be stored exactly as it was sent and returned unchanged until it is
    deleted; invalid input must be rejected without storing anything. Every scenario reads the data back,
    so it checks what was persisted, not only what the response echoed.

Rule: POST /pet stores the pet exactly as it was sent

    Scenario: A pet can be created with all fields and is stored
        When pet is created with the following values via POST /pet
            | Field    | Value                        |
            | Id       | {run_id_int}12345            |
            | Name     | Baily                        |
            | Status   | available                    |
            | Category | dogs                         |
            | PhotoUrl | https://example.com/dog1.jpg |
            | PhotoUrl | https://example.com/dog2.jpg |
            | Tag      | 1:friendly                   |
            | Tag      | 2:vaccinated                 |
            | Tag      | 3:small                      |
        Then the response should have '200' status code and contain the following values
            | Field     | Value                                                       |
            | Id        | {run_id_int}12345                                           |
            | Name      | Baily                                                       |
            | Status    | available                                                   |
            | Category  | dogs                                                        |
            | PhotoUrls | https://example.com/dog1.jpg ↕ https://example.com/dog2.jpg |
            | Tags      | 1:friendly ↕ 2:vaccinated ↕ 3:small                         |
        When the created pet named 'Baily' is retrieved via GET /pet/{id}
        Then the response should have '200' status code and match the created pet named 'Baily'

    # One example per value of the status enum (available / pending / sold). Deletion does not depend on
    # the status, so it is covered once, in the DELETE rule.
    Scenario Outline: A pet can be created with each status value
        When pet is created with the following values via POST /pet
            | Field  | Value            |
            | Id     | {run_id_int}<id> |
            | Name   | Rex              |
            | Status | <status>         |
        Then the response should have '200' status code and contain the following values
            | Field  | Value            |
            | Id     | {run_id_int}<id> |
            | Status | <status>         |
        When the last created pet is retrieved via GET /pet/{id}
        Then the response should have '200' status code and match the last created pet

        Examples:
            | status    | id    |
            | available | 20001 |
            | pending   | 20002 |
            | sold      | 20003 |

    # A second pet with the same tag (same tag id) and photo URL must not change the first pet
    # or take them away from it.
    Scenario: Tags and photo URLs can be reused by several pets
        Given pet is created with the following values via POST /pet
            | Field    | Value                          |
            | Id       | {run_id_int}11111              |
            | Name     | Rex                            |
            | PhotoUrl | https://example.com/shared.jpg |
            | Tag      | 1:friendly                     |
        When pet is created with the following values via POST /pet
            | Field    | Value                          |
            | Id       | {run_id_int}22222              |
            | Name     | Max                            |
            | PhotoUrl | https://example.com/shared.jpg |
            | Tag      | 1:friendly                     |
        Then the response should have '200' status code and contain the following values
            | Field     | Value                          |
            | Id        | {run_id_int}22222              |
            | Name      | Max                            |
            | PhotoUrls | https://example.com/shared.jpg |
            | Tags      | 1:friendly                     |
        When the created pet named 'Rex' is retrieved via GET /pet/{id}
        Then the response should have '200' status code and match the created pet named 'Rex'
        When the created pet named 'Max' is retrieved via GET /pet/{id}
        Then the response should have '200' status code and match the created pet named 'Max'

    # No Id is given (it is sent as null, which the API treats the same as a missing id), so the backend
    # must assign a distinct one per pet. Each GET by the generated Id must return that pet, not the other one.
    # BUG-002: the API returns 9223372036854775807 for every such pet, so the second one overwrites the first.
    @bug:BUG-002
    Scenario: Pets created without an ID get their own generated IDs
        When pet is created with the following values via POST /pet
            | Field    | Value     |
            | Name     | Toothless |
            | Category | DRAGONS   |
        Then the response should have '200' status code and contain the following values
            | Field     | Value          |
            | Id        | {valid-number} |
            | Name      | Toothless      |
            | Status    | null           |
            | Category  | DRAGONS        |
            | PhotoUrls | []             |
            | Tags      | []             |
        When pet is created with the following values via POST /pet
            | Field    | Value      |
            | Name     | Stormfly   |
            | Category | NIGHT_FURY |
        Then the response should have '200' status code and contain the following values
            | Field    | Value          |
            | Id       | {valid-number} |
            | Name     | Stormfly       |
            | Category | NIGHT_FURY     |
        And the created pets should have different IDs
        When the created pet named 'Toothless' is retrieved via GET /pet/{id}
        Then the response should have '200' status code and match the created pet named 'Toothless'
        When the created pet named 'Stormfly' is retrieved via GET /pet/{id}
        Then the response should have '200' status code and match the created pet named 'Stormfly'

    # BUG-001: the API answers 200 and silently overwrites the existing pet (upsert).
    @bug:BUG-001
    Scenario: Creating a pet with an existing ID is rejected and the existing pet is kept
        Given pet is created with the following values via POST /pet
            | Field  | Value             |
            | Id     | {run_id_int}12348 |
            | Name   | First             |
            | Status | available         |
        When pet is created with the following values via POST /pet
            | Field  | Value             |
            | Id     | {run_id_int}12348 |
            | Name   | Second            |
            | Status | sold              |
        Then the response status should be '409'
        When the created pet named 'First' is retrieved via GET /pet/{id}
        Then the response should have '200' status code and match the created pet named 'First'

    # The spec marks name as required and status as an enum; for invalid input it defines 405 Invalid input.
    # BUG-003: the API accepts both pets below with 200 and stores them.
    @bug:BUG-003
    Scenario Outline: An invalid pet is rejected and nothing is stored
        When pet is created with the following values via POST /pet
            | Field  | Value            |
            | Id     | {run_id_int}<id> |
            | Name   | <name>           |
            | Status | <status>         |
        Then the response status should be '405'
        When the pet with ID '{run_id_int}<id>' is retrieved via GET /pet/{id}
        Then the response status should be '404'
        And the response message should be 'Pet not found'

        Examples:
            | case                  | name | status    | id    |
            | missing required name | null | available | 30001 |
            | status not in enum    | Rex  | flying    | 30002 |

Rule: GET /pet/{id} returns the stored pet or a clear error
    The found case is checked by every POST scenario: each created pet is read back and must match.

    # The pet is created and deleted first, so its Id is known not to exist (a guessed Id could belong to
    # anyone on the shared server).
    Scenario: Getting a pet that does not exist returns 404
        Given pet is created with the following values via POST /pet
            | Field | Value             |
            | Id    | {run_id_int}99999 |
            | Name  | Ghost             |
        And the last created pet is deleted via DELETE /pet/{id}
        When the last created pet is retrieved via GET /pet/{id}
        Then the response status should be '404'
        And the response message should be 'Pet not found'

    # The spec defines 400 Invalid ID supplied for this case.
    # BUG-004: the API answers 404 with the server's Java exception text as the message.
    @bug:BUG-004
    Scenario: Getting a pet by a non-numeric ID is rejected as an invalid ID
        When the pet with ID 'abc' is retrieved via GET /pet/{id}
        Then the response status should be '400'
        And the response message should be 'Invalid ID supplied'

Rule: DELETE /pet/{id} removes exactly the requested pet

    Scenario: Deleting a pet removes only that pet, and deleting it again returns 404
        Given pet is created with the following values via POST /pet
            | Field | Value             |
            | Id    | {run_id_int}40001 |
            | Name  | Rex               |
        And pet is created with the following values via POST /pet
            | Field | Value             |
            | Id    | {run_id_int}40002 |
            | Name  | Max               |
        When the pet with ID '{run_id_int}40001' is deleted via DELETE /pet/{id}
        Then the response status should be '200'
        And the response message should be '{run_id_int}40001'
        When the pet with ID '{run_id_int}40001' is retrieved via GET /pet/{id}
        Then the response status should be '404'
        And the response message should be 'Pet not found'
        When the created pet named 'Max' is retrieved via GET /pet/{id}
        Then the response should have '200' status code and match the created pet named 'Max'
        # Deleting again is the same input class as deleting a pet that never existed: the Id is not stored.
        When the created pet named 'Rex' is deleted via DELETE /pet/{id}
        Then the response status should be '404'
        And the response body should be empty
