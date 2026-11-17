@pet @e2e
Feature: Pet end to end
    What POST /pet stores and GET /pet/{id} returns, one valid partition per scenario: required fields only,
    all fields, each status, generated Ids, tags and photo URLs shared by two pets; names in English and
    Ukrainian with emoji, digits and brackets, tags also in Spanish and French with special characters (& " % ,).

@smoke
Scenario: A pet can be created with the required fields only
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}10001 |
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}10001           |
        | Name     | Rex                         |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | {run_id_int}10001           |
        | Name     | Rex                         |
        | PhotoUrl | https://example.com/pet.jpg |
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | {run_id_int}10001           |
        | Name     | Rex                         |
        | PhotoUrl | https://example.com/pet.jpg |

@smoke
Scenario: A pet can be created with all fields
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}12345 |
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
    When the last created pet is retrieved via GET /pet/{id}
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

Scenario Outline: A pet can be created with each status value
    Given the pets with the following Ids do not exist
        | Id               |
        | {run_id_int}<id> |
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

    Examples:
        | case                    | id    | name          | status    |
        | available, English name | 20001 | Hachiko       | available |
        | pending, Ukrainian name | 20002 | Кіт у чоботях | pending   |
        | sold, name with emoji   | 20003 | Smaug 🐉      | sold      |

Scenario Outline: A pet created without a positive Id gets a generated Id
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | <id>                        |
        | Name     | <name>                      |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code and the returned pet should be exactly, with a generated Id
        | Field    | Value                       |
        | Name     | <name>                      |
        | PhotoUrl | https://example.com/pet.jpg |
    When the created pet named '<name>' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly, with the Id of the created pet named '<name>'
        | Field    | Value                       |
        | Name     | <name>                      |
        | PhotoUrl | https://example.com/pet.jpg |

    @smoke
    Examples: Id not sent
        | case        | id   | name      |
        | Id not sent | null | Toothless |

    Examples: Id not positive
        | case  | id | name     |
        | Id 0  | 0  | Stormfly |
        | Id -1 | -1 | Hookfang |

Scenario: Tags and photo URLs can be shared by two pets and stay after one pet is deleted
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}11111 |
        | {run_id_int}22222 |
    When a pet is created with the following values via POST /pet
        | Field    | Value                          |
        | Id       | {run_id_int}11111              |
        | Name     | Garfield                       |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:muy cariñoso & très fluffy   |
    Then the response should have '200' status code
    When a pet is created with the following values via POST /pet
        | Field    | Value                          |
        | Id       | {run_id_int}22222              |
        | Name     | Beethoven (1992)               |
        | PhotoUrl | https://example.com/shared.jpg |
        | Tag      | 1:muy cariñoso & très fluffy   |
    Then the response should have '200' status code
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
Scenario: Pets created without an Id or with Id 0 get different generated Ids
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
        | Id       | 0                           |
        | Name     | Stormfly                    |
        | Category | NIGHT_FURY                  |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code and the returned pet should be exactly, with a generated Id
        | Field    | Value                       |
        | Name     | Stormfly                    |
        | Category | 0:NIGHT_FURY                |
        | PhotoUrl | https://example.com/pet.jpg |
    And the created pets should have different Ids
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
