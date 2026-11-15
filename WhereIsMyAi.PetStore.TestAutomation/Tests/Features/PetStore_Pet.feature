Feature: Pet endpoints
    Covers POST /pet, GET /pet/{id} and DELETE /pet/{id}: the stored pet equals what was sent, generated and
    duplicate Ids, repeated deletes, tags and photo URLs shared by two pets, invalid input, invalid and missing Ids,
    boundary Ids and name lengths; names in English and Ukrainian with emoji, tags also in Spanish and French with
    special characters (& " ( ) % ,).

Scenario: A pet can be created with all fields and deleted only once
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

Scenario Outline: A pet can be created and deleted with each status value
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}<id>            |
        | Name     | <name>                      |
        | Status   | <status>                    |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | {run_id_int}<id>            |
        | Name     | <name>                      |
        | Status   | <status>                    |
        | PhotoUrl | https://example.com/pet.jpg |
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | {run_id_int}<id>            |
        | Name     | <name>                      |
        | Status   | <status>                    |
        | PhotoUrl | https://example.com/pet.jpg |
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

Scenario Outline: A pet can be created and deleted with boundary Ids and name lengths
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | <id>                        |
        | Name     | <name>                      |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | <id>                        |
        | Name     | <name>                      |
        | PhotoUrl | https://example.com/pet.jpg |
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | <id>                        |
        | Name     | <name>                      |
        | PhotoUrl | https://example.com/pet.jpg |
    When the last created pet is deleted via DELETE /pet/{id}
    Then the response should have '200' status code
    And the response message should be '<id>'

    Examples:
        | case                               | id                         | name                                                                                                                                                                                                                                                             |
        | 18-digit Id (above 2^53)           | {run_id_int}00000000000001 | Salem                                                                                                                                                                                                                                                            |
        | empty name                         | {run_id_int}50001          |                                                                                                                                                                                                                                                                  |
        | 256-character name                 | {run_id_int}50002          | AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA |

    # Id 1 cannot take the {run_id_int} prefix and can be shared with other users of the public server.
    @shared-ids
    Examples: Id shared with other users
        | case                 | id | name   |
        | smallest positive Id | 1  | Lassie |

Scenario: Tags and photo URLs can be shared by two pets and stay after one pet is deleted
    When a pet is created with the following values via POST /pet
        | Field    | Value                          |
        | Id       | {run_id_int}11111              |
        | Name     | Garfield                       |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:muy cariñoso & très fluffy   |
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                          |
        | Id       | {run_id_int}11111              |
        | Name     | Garfield                       |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:muy cariñoso & très fluffy   |
    When a pet is created with the following values via POST /pet
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
        | Field    | Value                       |
        | Name     | Toothless                   |
        | Category | DRAGONS                     |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code and the returned pet should be exactly, with a generated Id
        | Field    | Value                       |
        | Name     | Toothless                   |
        | Category | 0:DRAGONS                   |
        | PhotoUrl | https://example.com/pet.jpg |
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Name     | Stormfly                    |
        | Category | NIGHT_FURY                  |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code and the returned pet should be exactly, with a generated Id
        | Field    | Value                       |
        | Name     | Stormfly                    |
        | Category | 0:NIGHT_FURY                |
        | PhotoUrl | https://example.com/pet.jpg |
    And the created pets should have different IDs
    When the created pet named 'Toothless' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly, with the Id of the created pet named 'Toothless'
        | Field    | Value                       |
        | Name     | Toothless                   |
        | Category | 0:DRAGONS                   |
        | PhotoUrl | https://example.com/pet.jpg |
    When the created pet named 'Stormfly' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly, with the Id of the created pet named 'Stormfly'
        | Field    | Value                       |
        | Name     | Stormfly                    |
        | Category | 0:NIGHT_FURY                |
        | PhotoUrl | https://example.com/pet.jpg |

@bug:BUG-001
Scenario: Creating a pet with an existing ID is rejected with 409 Conflict
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}12348           |
        | Name     | Norbert                     |
        | Status   | available                   |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}12348           |
        | Name     | Mushu                       |
        | Status   | sold                        |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '409' status code

@bug:BUG-001
Scenario: Creating a pet with an existing ID keeps the existing pet
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}12348           |
        | Name     | Norbert                     |
        | Status   | available                   |
        | PhotoUrl | https://example.com/pet.jpg |
    And a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}12348           |
        | Name     | Mushu                       |
        | Status   | sold                        |
        | PhotoUrl | https://example.com/pet.jpg |
    When the created pet named 'Norbert' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | {run_id_int}12348           |
        | Name     | Norbert                     |
        | Status   | available                   |
        | PhotoUrl | https://example.com/pet.jpg |

@bug:BUG-003
Scenario Outline: An invalid pet is rejected with 405 Invalid input and nothing is stored
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}<id>            |
        | Name     | <name>                      |
        | Status   | <status>                    |
        | PhotoUrl | <photoUrl>                  |
    Then the response should have '405' status code
    And the response message should be 'Invalid input'
    When the pet with ID '{run_id_int}<id>' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    And the response message should be 'Pet not found'

    Examples:
        | case                       | name | status    | photoUrl                    | id    |
        | missing required name      | null | available | https://example.com/pet.jpg | 30001 |
        | status not in enum         | Rex  | flying    | https://example.com/pet.jpg | 30002 |
        | missing required photoUrls | Rex  | available | null                        | 30003 |

@bug:BUG-004
Scenario Outline: Getting a pet by an invalid ID is rejected with 400 Invalid ID supplied
    When the pet with ID '<id>' is retrieved via GET /pet/{id}
    Then the response should have '400' status code
    And the response message should be 'Invalid ID supplied'

    Examples:
        | case              | id                  |
        | non-numeric       | abc                 |
        | int64 maximum + 1 | 9223372036854775808 |

@bug:BUG-004
Scenario Outline: Deleting a pet by an invalid ID is rejected with 400 Invalid ID supplied
    When the pet with ID '<id>' is deleted via DELETE /pet/{id}
    Then the response should have '400' status code
    And the response message should be 'Invalid ID supplied'

    Examples:
        | case              | id                  |
        | non-numeric       | abc                 |
        | int64 maximum + 1 | 9223372036854775808 |

@bug:BUG-005
Scenario: Deleting a pet that does not exist is answered with 404 Pet not found
    When the pet with ID '{run_id_int}99999' is deleted via DELETE /pet/{id}
    Then the response should have '404' status code
    And the response message should be 'Pet not found'
