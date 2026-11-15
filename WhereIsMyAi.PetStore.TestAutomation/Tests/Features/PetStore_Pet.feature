Feature: Pet endpoints
    Covers POST /pet, GET /pet/{id} and DELETE /pet/{id}: the stored pet equals what was sent, repeated deletes,
    tags and photo URLs shared by two pets, invalid input, boundary Ids and names, and names and tags in English,
    Ukrainian, Spanish and French with emoji and special characters (& " ( ) % ,).

Scenario: A pet can be created with all fields and deleted
    When a pet is created with the following values via POST /pet
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
    Then the response should have '200' status code
    When the created pet named 'Baily' is deleted via DELETE /pet/{id}
    Then the response should have '404' status code
    And the response body should be empty

Scenario Outline: A pet can be created and deleted with each status value
    When a pet is created with the following values via POST /pet
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
    Then the response should have '200' status code
    And the response message should be '{run_id_int}<id>'
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    And the response message should be 'Pet not found'

    Examples:
        | status    | name          | id    |
        | available | Hachiko       | 20001 |
        | pending   | Кіт у чоботях | 20002 |
        | sold      | Smaug 🐉      | 20003 |

# Boundary Ids cannot take the {run_id_int} prefix and can be shared with other users of the public server.
@shared-ids
Scenario Outline: A pet can be created and deleted with boundary values
    When a pet is created with the following values via POST /pet
        | Field | Value  |
        | Id    | <id>   |
        | Name  | <name> |
    Then the response should have '200' status code and the returned pet should be exactly
        | Field | Value  |
        | Id    | <id>   |
        | Name  | <name> |
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field | Value  |
        | Id    | <id>   |
        | Name  | <name> |
    When the last created pet is deleted via DELETE /pet/{id}
    Then the response should have '200' status code
    And the response message should be '<id>'

    Examples:
        | case                               | id                  | name                                                                                                                                                                                                                                                             |
        | smallest positive Id               | 1                   | Lassie                                                                                                                                                                                                                                                           |
        | largest Id below the int64 maximum | 9223372036854775806 | Salem                                                                                                                                                                                                                                                            |
        | empty name                         | {run_id_int}50001   | {empty}                                                                                                                                                                                                                                                          |
        | 256-character name                 | {run_id_int}50002   | AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA |

Scenario: Tags and photo URLs can be reused by several pets and stay after one pet is deleted
    When a pet is created with the following values via POST /pet
        | Field    | Value                          |
        | Id       | {run_id_int}11111              |
        | Name     | Garfield                       |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:muy cariñoso & très fluffy   |
    And a pet is created with the following values via POST /pet
        | Field    | Value                          |
        | Id       | {run_id_int}22222              |
        | Name     | Beethoven (1992)               |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:muy cariñoso & très fluffy   |
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                          |
        | Id       | {run_id_int}22222              |
        | Name     | Beethoven (1992)               |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:muy cariñoso & très fluffy   |
    When the created pet named 'Garfield' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                          |
        | Id       | {run_id_int}11111              |
        | Name     | Garfield                       |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:muy cariñoso & très fluffy   |
    When the created pet named 'Garfield' is deleted via DELETE /pet/{id}
    Then the response should have '200' status code
    When the created pet named 'Beethoven (1992)' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                          |
        | Id       | {run_id_int}22222              |
        | Name     | Beethoven (1992)               |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:muy cariñoso & très fluffy   |

@bug:BUG-002
Scenario: Pets created without an ID get their own generated IDs
    When a pet is created with the following values via POST /pet
        | Field    | Value     |
        | Name     | Toothless |
        | Category | DRAGONS   |
    Then the response should have '200' status code and the returned pet should be exactly, with a generated Id
        | Field    | Value     |
        | Name     | Toothless |
        | Category | 0:DRAGONS |
    When a pet is created with the following values via POST /pet
        | Field    | Value      |
        | Name     | Stormfly   |
        | Category | NIGHT_FURY |
    Then the response should have '200' status code and the returned pet should be exactly, with a generated Id
        | Field    | Value        |
        | Name     | Stormfly     |
        | Category | 0:NIGHT_FURY |
    And the created pets should have different IDs
    When the created pet named 'Toothless' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly, with the Id of the created pet named 'Toothless'
        | Field    | Value     |
        | Name     | Toothless |
        | Category | 0:DRAGONS |
    When the created pet named 'Stormfly' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly, with the Id of the created pet named 'Stormfly'
        | Field    | Value        |
        | Name     | Stormfly     |
        | Category | 0:NIGHT_FURY |

@bug:BUG-001
Scenario: Creating a pet with an existing ID is rejected and the existing pet is kept
    When a pet is created with the following values via POST /pet
        | Field  | Value             |
        | Id     | {run_id_int}12348 |
        | Name   | Norbert           |
        | Status | available         |
    And a pet is created with the following values via POST /pet
        | Field  | Value             |
        | Id     | {run_id_int}12348 |
        | Name   | Mushu             |
        | Status | sold              |
    Then the response should have '409' status code
    When the created pet named 'Norbert' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field  | Value             |
        | Id     | {run_id_int}12348 |
        | Name   | Norbert           |
        | Status | available         |

@bug:BUG-003
Scenario Outline: An invalid pet is rejected and nothing is stored
    When a pet is created with the following values via POST /pet
        | Field  | Value            |
        | Id     | {run_id_int}<id> |
        | Name   | <name>           |
        | Status | <status>         |
    Then the response should have '405' status code
    When the pet with ID '{run_id_int}<id>' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    And the response message should be 'Pet not found'

    Examples:
        | case                  | name | status    | id    |
        | missing required name |      | available | 30001 |
        | status not in enum    | Rex  | flying    | 30002 |

@bug:BUG-004
Scenario: Getting a pet by a non-numeric ID is rejected as an invalid ID
    When the pet with ID 'abc' is retrieved via GET /pet/{id}
    Then the response should have '400' status code
    And the response message should be 'Invalid ID supplied'
