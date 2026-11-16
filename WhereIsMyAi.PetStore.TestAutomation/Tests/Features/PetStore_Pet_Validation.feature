@pet @validation
Feature: Pet input validation
    Which inputs POST /pet, GET /pet/{id} and DELETE /pet/{id} accept and which they reject: boundary Ids and
    name lengths, invalid pets, invalid path Ids.

Scenario Outline: A pet can be created and deleted with boundary Ids and name lengths
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
        | status not in enum         | 30002 | Rex  | flying    | https://example.com/pet.jpg |
        | missing required photoUrls | 30003 | Rex  | available | null                        |

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
