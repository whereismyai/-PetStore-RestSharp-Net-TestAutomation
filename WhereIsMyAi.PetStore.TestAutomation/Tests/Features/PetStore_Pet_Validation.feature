@pet @validation
Feature: Pet input validation
    Which inputs POST /pet, GET /pet/{id} and DELETE /pet/{id} accept and which they reject, one partition or
    boundary per test case: Ids, name lengths, empty lists, missing required fields, invalid status values,
    values of the wrong JSON type, an empty or broken body, invalid path Ids.

Scenario Outline: A pet can be created with a boundary Id
    Given the pets with the following Ids do not exist
        | Id   |
        | <id> |
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

    Examples:
        | case                     | id                         | name  |
        | 18-digit Id (above 2^53) | {run_id_int}00000000000001 | Salem |

    # Id 1 cannot take the {run_id_int} prefix and can be shared with other users of the public server.
    @shared-ids
    Examples: Id shared with other users
        | case                 | id | name   |
        | smallest positive Id | 1  | Lassie |

Scenario Outline: A pet can be created with a name of any length
    Given the pets with the following Ids do not exist
        | Id               |
        | {run_id_int}<id> |
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}<id>            |
        | Name     | <name>                      |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | {run_id_int}<id>            |
        | Name     | <name>                      |
        | PhotoUrl | https://example.com/pet.jpg |

    Examples:
        | case               | id    | name                                                                                                                                                                                                                                                             |
        | empty name         | 50001 |                                                                                                                                                                                                                                                                  |
        | 1-character name   | 50002 | Q                                                                                                                                                                                                                                                                |
        | 256-character name | 50003 | AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA |

Scenario Outline: A pet can be created with an empty list
    Given the pets with the following Ids do not exist
        | Id               |
        | {run_id_int}<id> |
    When a pet is created with the following 'application/json' body via POST /pet
        """
        {"id": {run_id_int}<id>, "name": "Rex", <lists>}
        """
    Then the response should have '200' status code
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value            |
        | Id       | {run_id_int}<id> |
        | Name     | Rex              |
        | PhotoUrl | <photoUrl>       |

    Examples:
        | case            | id    | lists                                                    | photoUrl                    |
        | empty photoUrls | 51001 | "photoUrls": []                                          | null                        |
        | empty tags      | 51002 | "photoUrls": ["https://example.com/pet.jpg"], "tags": [] | https://example.com/pet.jpg |

@bug:BUG-003
Scenario Outline: An invalid pet is rejected with 405 Invalid input and not stored
    Given the pets with the following Ids do not exist
        | Id               |
        | {run_id_int}<id> |
    When a pet is created with the following values via POST /pet
        | Field    | Value            |
        | Id       | {run_id_int}<id> |
        | Name     | <name>           |
        | Status   | <status>         |
        | PhotoUrl | <photoUrl>       |
    Then the response should have '405' status code
    When the pet with Id '{run_id_int}<id>' is retrieved via GET /pet/{id}
    Then the response should have '404' status code

    Examples:
        | case                       | id    | name | status    | photoUrl                    |
        | missing required name      | 30001 | null | available | https://example.com/pet.jpg |
        | missing required photoUrls | 30002 | Rex  | available | null                        |
        | missing name and photoUrls | 30003 | null | available | null                        |
        | status not in enum         | 30004 | Rex  | flying    | https://example.com/pet.jpg |
        | status in the wrong case   | 30005 | Rex  | Available | https://example.com/pet.jpg |
        | empty status               | 30006 | Rex  |           | https://example.com/pet.jpg |

@bug:BUG-007
Scenario Outline: A pet with a field of the wrong JSON type is rejected with 405 Invalid input and not stored
    Given the pets with the following Ids do not exist
        | Id               |
        | {run_id_int}<id> |
    When a pet is created with the following 'application/json' body via POST /pet
        """
        {"id": {run_id_int}<id>, "name": "Rex", "photoUrls": <photoUrls>, "category": <category>, "tags": <tags>}
        """
    Then the response should have '405' status code
    When the pet with Id '{run_id_int}<id>' is retrieved via GET /pet/{id}
    Then the response should have '404' status code

    Examples:
        | case                  | id    | photoUrls                       | category | tags   |
        | photoUrls is a string | 31001 | "https://example.com/pet.jpg"   | null     | null   |
        | category is a string  | 31002 | ["https://example.com/pet.jpg"] | "dogs"   | null   |
        | tags is a string      | 31003 | ["https://example.com/pet.jpg"] | null     | "cute" |

@bug:BUG-007
Scenario Outline: A body that is not a pet is rejected with 405 Invalid input
    When a pet is created with the following 'application/json' body via POST /pet
        """
        <body>
        """
    Then the response should have '405' status code

    Examples:
        | case           | body                                                                       |
        | non-numeric Id | {"id": "abc", "name": "Rex", "photoUrls": ["https://example.com/pet.jpg"]} |
        | array of pets  | [{"name": "Rex", "photoUrls": ["https://example.com/pet.jpg"]}]            |

@smoke
Scenario: A pet without data is rejected with 405 Invalid input
    When a pet is created with an empty 'application/json' body via POST /pet
    Then the response should have '405' status code

@smoke
Scenario: A pet in broken JSON is rejected with 400
    When a pet is created with the following 'application/json' body via POST /pet
        """
        {"name": "Rex", "photoUrls": ["https://example.com/pet.jpg"],
        """
    Then the response should have '400' status code

@bug:BUG-004
Scenario Outline: Getting a pet by an invalid Id is rejected with 400
    When the pet with Id '<id>' is retrieved via GET /pet/{id}
    Then the response should have '400' status code

    Examples:
        | case              | id                  |
        | non-numeric       | abc                 |
        | int64 maximum + 1 | 9223372036854775808 |

@bug:BUG-004
Scenario Outline: Deleting a pet by an invalid Id is rejected with 400
    When the pet with Id '<id>' is deleted via DELETE /pet/{id}
    Then the response should have '400' status code

    Examples:
        | case              | id                  |
        | non-numeric       | abc                 |
        | int64 maximum + 1 | 9223372036854775808 |
