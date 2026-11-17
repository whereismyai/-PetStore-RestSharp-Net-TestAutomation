@pet @e2e
Feature: Pet end to end
    What POST /pet stores, GET /pet/{id} returns and DELETE /pet/{id} removes: required fields only, all fields,
    each status, boundary Ids, a long name, generated Ids, tags and photo URLs shared by two pets; names in
    English and Ukrainian with emoji and brackets, tags also in Spanish and French with special characters.

Scenario Outline: A pet can be created, read back and deleted
    Given the pets with the following Ids do not exist
        | Id   |
        | <id> |
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | <id>                        |
        | Name     | <name>                      |
        | Status   | <status>                    |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | <id>                        |
        | Name     | <name>                      |
        | Status   | <status>                    |
        | PhotoUrl | https://example.com/pet.jpg |
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | <id>                        |
        | Name     | <name>                      |
        | Status   | <status>                    |
        | PhotoUrl | https://example.com/pet.jpg |
    When the last created pet is deleted via DELETE /pet/{id}
    Then the response should have '200' status code
    And the response message should be '<id>'
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    And the response message should be 'Pet not found'

    @smoke
    Examples: Required fields only
        | case                 | id                | name | status |
        | required fields only | {run_id_int}10001 | Rex  | null   |

    Examples: Status values, names and Ids
        | case                               | id                         | name                                                                                                                                                                                                                                                             | status    |
        | available, 256-character name      | {run_id_int}10002          | AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA | available |
        | pending, Ukrainian name with emoji | {run_id_int}10003          | Кіт у чоботях 🐈                                                                                                                                                                                                                                                 | pending   |
        | sold, 18-digit Id                  | {run_id_int}00000000000001 | Salem                                                                                                                                                                                                                                                            | sold      |

    # Id 1 cannot take the {run_id_int} prefix and can be shared with other users of the public server.
    @shared-ids
    Examples: Id shared with other users
        | case                 | id | name   | status |
        | smallest positive Id | 1  | Lassie | null   |

@smoke
Scenario: A pet created without an Id or with Id 0 gets a generated Id
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Name     | Toothless                   |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code and the returned pet should be exactly, with a generated Id
        | Field    | Value                       |
        | Name     | Toothless                   |
        | PhotoUrl | https://example.com/pet.jpg |
    When the created pet named 'Toothless' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly, with the Id of the created pet named 'Toothless'
        | Field    | Value                       |
        | Name     | Toothless                   |
        | PhotoUrl | https://example.com/pet.jpg |
    When the created pet named 'Toothless' is deleted via DELETE /pet/{id}
    Then the response should have '200' status code
    When the created pet named 'Toothless' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | 0                           |
        | Name     | Stormfly                    |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code and the returned pet should be exactly, with a generated Id
        | Field    | Value                       |
        | Name     | Stormfly                    |
        | PhotoUrl | https://example.com/pet.jpg |
    When the created pet named 'Stormfly' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly, with the Id of the created pet named 'Stormfly'
        | Field    | Value                       |
        | Name     | Stormfly                    |
        | PhotoUrl | https://example.com/pet.jpg |

Scenario: Two pets can share tags and photo URLs, and deleting one leaves the other unchanged
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}11111 |
        | {run_id_int}22222 |
    When a pet is created with the following values via POST /pet
        | Field    | Value                          |
        | Id       | {run_id_int}11111              |
        | Name     | Garfield                       |
        | Status   | available                      |
        | Category | 1:cats                         |
        | PhotoUrl | https://example.com/cat1.jpg   |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:friendly                     |
        | Tag      | 2:з щепленням, 100%            |
        | Tag      | 3:muy cariñoso & très "fluffy" |
    Then the response should have '200' status code
    When the created pet named 'Garfield' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                          |
        | Id       | {run_id_int}11111              |
        | Name     | Garfield                       |
        | Status   | available                      |
        | Category | 1:cats                         |
        | PhotoUrl | https://example.com/cat1.jpg   |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:friendly                     |
        | Tag      | 2:з щепленням, 100%            |
        | Tag      | 3:muy cariñoso & très "fluffy" |
    When a pet is created with the following values via POST /pet
        | Field    | Value                          |
        | Id       | {run_id_int}22222              |
        | Name     | Beethoven (1992)               |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 3:muy cariñoso & très "fluffy" |
    Then the response should have '200' status code
    When the created pet named 'Garfield' is deleted via DELETE /pet/{id}
    Then the response should have '200' status code
    When the created pet named 'Garfield' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    When the created pet named 'Beethoven (1992)' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                          |
        | Id       | {run_id_int}22222              |
        | Name     | Beethoven (1992)               |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 3:muy cariñoso & très "fluffy" |

@bug:BUG-002
Scenario: Pets created without a positive Id get different generated Ids
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Name     | Toothless                   |
        | Category | DRAGONS                     |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | -1                          |
        | Name     | Hookfang                    |
        | Category | NIGHT_FURY                  |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code
    And the created pets should have different Ids
    When the created pet named 'Toothless' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly, with the Id of the created pet named 'Toothless'
        | Field    | Value                       |
        | Name     | Toothless                   |
        | Category | 0:DRAGONS                   |
        | PhotoUrl | https://example.com/pet.jpg |
    When the created pet named 'Hookfang' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly, with the Id of the created pet named 'Hookfang'
        | Field    | Value                       |
        | Name     | Hookfang                    |
        | Category | 0:NIGHT_FURY                |
        | PhotoUrl | https://example.com/pet.jpg |
