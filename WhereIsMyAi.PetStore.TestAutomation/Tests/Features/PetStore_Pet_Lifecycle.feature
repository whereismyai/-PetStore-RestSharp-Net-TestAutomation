@pet @lifecycle
Feature: Pet lifecycle
    The states a pet goes through (does not exist, exists, deleted) and the transitions between them via
    POST /pet, GET /pet/{id} and DELETE /pet/{id}: allowed ones, and requests on a pet that is not there.

Scenario: A deleted pet can no longer be retrieved or deleted
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}40001 |
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}40001           |
        | Name     | Bolt                        |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code
    When the last created pet is deleted via DELETE /pet/{id}
    Then the response should have '200' status code
    And the response message should be '{run_id_int}40001'
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    And the response message should be 'Pet not found'
    When the last created pet is deleted via DELETE /pet/{id}
    Then the response should have '404' status code

Scenario: A pet can be created again with the Id of a deleted pet
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}40002 |
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}40002           |
        | Name     | Marley                      |
        | PhotoUrl | https://example.com/pet.jpg |
    And the created pet named 'Marley' is deleted via DELETE /pet/{id}
    And a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}40002           |
        | Name     | Beethoven                   |
        | Status   | sold                        |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code
    When the created pet named 'Beethoven' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | {run_id_int}40002           |
        | Name     | Beethoven                   |
        | Status   | sold                        |
        | PhotoUrl | https://example.com/pet.jpg |

Scenario: Getting a pet that was never created returns 404 Pet not found
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}40003 |
    When the pet with Id '{run_id_int}40003' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    And the response message should be 'Pet not found'

Scenario: Deleting a pet that was never created returns 404
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}40004 |
    When the pet with Id '{run_id_int}40004' is deleted via DELETE /pet/{id}
    Then the response should have '404' status code

Scenario Outline: Getting and deleting a pet by an Id no pet can have returns 404
    When the pet with Id '<id>' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    And the response message should be 'Pet not found'
    When the pet with Id '<id>' is deleted via DELETE /pet/{id}
    Then the response should have '404' status code

    Examples:
        | case        | id |
        | Id 0        | 0  |
        | negative Id | -1 |

@bug:BUG-001
Scenario: Creating a pet with an existing Id is rejected with 409 Conflict
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}12348 |
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
Scenario: Creating a pet with an existing Id keeps the existing pet
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}12348 |
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
