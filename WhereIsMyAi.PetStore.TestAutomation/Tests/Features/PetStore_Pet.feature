Feature: Pet endpoints
    Every created pet is read back, so the checks cover what was stored, not only the response.

Scenario: A pet can be created with all fields and deleted
    When pet is created with the following values via POST /pet
        | Field    | Value                        |
        | Id       | {run_id_int}12345            |
        | Name     | Baily                        |
        | Status   | available                    |
        | Category | 1:dogs                       |
        | PhotoUrl | https://example.com/dog1.jpg |
        | PhotoUrl | https://example.com/dog2.jpg |
        | Tag      | 1:friendly                   |
        | Tag      | 2:з щепленням, 100%          |
        | Tag      | 3:small & "cute"             |
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                        |
        | Id       | {run_id_int}12345            |
        | Name     | Baily                        |
        | Status   | available                    |
        | Category | 1:dogs                       |
        | PhotoUrl | https://example.com/dog1.jpg |
        | PhotoUrl | https://example.com/dog2.jpg |
        | Tag      | 1:friendly                   |
        | Tag      | 2:з щепленням, 100%          |
        | Tag      | 3:small & "cute"             |
    When the created pet named 'Baily' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                        |
        | Id       | {run_id_int}12345            |
        | Name     | Baily                        |
        | Status   | available                    |
        | Category | 1:dogs                       |
        | PhotoUrl | https://example.com/dog1.jpg |
        | PhotoUrl | https://example.com/dog2.jpg |
        | Tag      | 1:friendly                   |
        | Tag      | 2:з щепленням, 100%          |
        | Tag      | 3:small & "cute"             |
    When the created pet named 'Baily' is deleted via DELETE /pet/{id}
    Then the response status should be '200'
    When the created pet named 'Baily' is deleted via DELETE /pet/{id}
    Then the response status should be '404'
    And the response body should be empty

Scenario Outline: A pet can be created and deleted with each status value
    When pet is created with the following values via POST /pet
        | Field  | Value            |
        | Id     | {run_id_int}<id> |
        | Name   | <name>           |
        | Status | <status>         |
    Then the response should have '200' status code and the returned pet should be exactly
        | Field  | Value            |
        | Id     | {run_id_int}<id> |
        | Name   | <name>           |
        | Status | <status>         |
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field  | Value            |
        | Id     | {run_id_int}<id> |
        | Name   | <name>           |
        | Status | <status>         |
    When the last created pet is deleted via DELETE /pet/{id}
    Then the response status should be '200'
    And the response message should be '{run_id_int}<id>'
    When the last created pet is retrieved via GET /pet/{id}
    Then the response status should be '404'
    And the response message should be 'Pet not found'

    Examples:
        | status    | name          | id    |
        | available | Hachiko       | 20001 |
        | pending   | Кіт у чоботях | 20002 |
        | sold      | Smaug 🐉      | 20003 |

Scenario: Tags and photo URLs can be reused by several pets and stay after one pet is deleted
    When pet is created with the following values via POST /pet
        | Field    | Value                          |
        | Id       | {run_id_int}11111              |
        | Name     | Garfield                       |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:très friendly & fluffy       |
    And pet is created with the following values via POST /pet
        | Field    | Value                          |
        | Id       | {run_id_int}22222              |
        | Name     | Beethoven (1992)               |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:très friendly & fluffy       |
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                          |
        | Id       | {run_id_int}22222              |
        | Name     | Beethoven (1992)               |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:très friendly & fluffy       |
    When the created pet named 'Garfield' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                          |
        | Id       | {run_id_int}11111              |
        | Name     | Garfield                       |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:très friendly & fluffy       |
    When the created pet named 'Garfield' is deleted via DELETE /pet/{id}
    Then the response status should be '200'
    When the created pet named 'Beethoven (1992)' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                          |
        | Id       | {run_id_int}22222              |
        | Name     | Beethoven (1992)               |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:très friendly & fluffy       |

@bug:BUG-002
Scenario: Pets created without an ID get their own generated IDs
    When pet is created with the following values via POST /pet
        | Field    | Value     |
        | Name     | Toothless |
        | Category | DRAGONS   |
    Then the response should have '200' status code and the returned pet should contain
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
    Then the response should have '200' status code and the returned pet should contain
        | Field    | Value          |
        | Id       | {valid-number} |
        | Name     | Stormfly       |
        | Category | NIGHT_FURY     |
    And the created pets should have different IDs
    When the created pet named 'Toothless' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should match the created pet named 'Toothless'
    When the created pet named 'Stormfly' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should match the created pet named 'Stormfly'

@bug:BUG-001
Scenario: Creating a pet with an existing ID is rejected and the existing pet is kept
    When pet is created with the following values via POST /pet
        | Field  | Value             |
        | Id     | {run_id_int}12348 |
        | Name   | Norbert           |
        | Status | available         |
    And pet is created with the following values via POST /pet
        | Field  | Value             |
        | Id     | {run_id_int}12348 |
        | Name   | Mushu             |
        | Status | sold              |
    Then the response status should be '409'
    When the created pet named 'Norbert' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field  | Value             |
        | Id     | {run_id_int}12348 |
        | Name   | Norbert           |
        | Status | available         |

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
