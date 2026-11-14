Feature: Pet endpoints
    Every created pet is read back, so the checks cover what was stored, not only the response.

Scenario: A pet can be created with all fields and deleted
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
    When the created pet named 'Baily' is deleted via DELETE /pet/{id}
    Then the response status should be '200'
    When the created pet named 'Baily' is deleted via DELETE /pet/{id}
    Then the response status should be '404'
    And the response body should be empty

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

Scenario: Tags and photo URLs can be reused by several pets and stay after one pet is deleted
    When pet is created with the following values via POST /pet
        | Field    | Value                          |
        | Id       | {run_id_int}11111              |
        | Name     | Rex                            |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:friendly                     |
    And pet is created with the following values via POST /pet
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
    When the created pet named 'Rex' is deleted via DELETE /pet/{id}
    Then the response status should be '200'
    When the created pet named 'Max' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and match the created pet named 'Max'

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

@bug:BUG-001
Scenario: Creating a pet with an existing ID is rejected and the existing pet is kept
    When pet is created with the following values via POST /pet
        | Field  | Value             |
        | Id     | {run_id_int}12348 |
        | Name   | First             |
        | Status | available         |
    And pet is created with the following values via POST /pet
        | Field  | Value             |
        | Id     | {run_id_int}12348 |
        | Name   | Second            |
        | Status | sold              |
    Then the response status should be '409'
    When the created pet named 'First' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and match the created pet named 'First'

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

@bug:BUG-004
Scenario: Getting a pet by a non-numeric ID is rejected as an invalid ID
    When the pet with ID 'abc' is retrieved via GET /pet/{id}
    Then the response status should be '400'
    And the response message should be 'Invalid ID supplied'
