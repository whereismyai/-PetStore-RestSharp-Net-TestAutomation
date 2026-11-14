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

    # One example per value of the status enum (available / pending / sold). Each pet is then deleted,
    # so deletion is covered for every status too.
    Scenario Outline: A pet can be created and deleted with each status value
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
        When the last created pet is deleted via DELETE /pet/{id}
        Then the response status should be '200'
        And the response message should be '{run_id_int}<id>'
        When the last created pet is retrieved via GET /pet/{id}
        Then the response status should be '404'
        And the response message should be 'Pet not found'

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

Rule: GET /pet/{id} returns the stored pet or a clear error
    The found case is checked by every POST scenario: each created pet is read back and must match.

    # An Id that was never created. The server is shared, so {run_id_int} keeps a collision unlikely.
    Scenario: Getting a pet that does not exist returns 404
        When the pet with ID '{run_id_int}99999' is retrieved via GET /pet/{id}
        Then the response status should be '404'
        And the response message should be 'Pet not found'

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
