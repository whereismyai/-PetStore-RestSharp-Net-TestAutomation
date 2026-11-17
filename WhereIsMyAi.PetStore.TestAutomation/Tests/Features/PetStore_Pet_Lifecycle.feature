@pet @lifecycle
Feature: Pet lifecycle
    The states a pet goes through (does not exist, exists, deleted) and the transitions between them via
    POST /pet, GET /pet/{id} and DELETE /pet/{id}: requests on a pet that is not there, reusing a freed Id,
    a duplicate Id. Deleting an existing pet is part of every scenario in PetStore_Pet_E2E.feature.

@smoke
Scenario: A pet that does not exist cannot be retrieved or deleted
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}40003 |
    When the pet with Id '{run_id_int}40003' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    And the response message should be 'Pet not found'
    When the pet with Id '{run_id_int}40003' is deleted via DELETE /pet/{id}
    Then the response should have '404' status code
    When the pet with Id '0' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    When the pet with Id '0' is deleted via DELETE /pet/{id}
    Then the response should have '404' status code
    When the pet with Id '-1' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    When the pet with Id '-1' is deleted via DELETE /pet/{id}
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
    Then the response should have '200' status code
    When the created pet named 'Marley' is deleted via DELETE /pet/{id}
    Then the response should have '200' status code
    When the pet with Id '{run_id_int}40002' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    When a pet is created with the following values via POST /pet
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

@bug:BUG-001
Scenario: Creating a pet with an existing Id is rejected with 409 Conflict and keeps the existing pet
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
    When the created pet named 'Norbert' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | {run_id_int}12348           |
        | Name     | Norbert                     |
        | Status   | available                   |
        | PhotoUrl | https://example.com/pet.jpg |
