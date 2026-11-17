@pet @validation
Feature: Pet input validation
    Which inputs POST /pet, GET /pet/{id} and DELETE /pet/{id} accept and which they reject: an empty optional
    list, required fields missing or empty, invalid status values, values of the wrong JSON type, an empty or broken body,
    invalid path Ids. Every rejected pet is looked up afterwards to prove it was not stored.

@smoke
Scenario: A request without a readable pet is rejected and nothing is stored
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}52001 |
    When a pet is created with an empty 'application/json' body via POST /pet
    Then the response should have '405' status code
    When a pet is created with the following 'application/json' body via POST /pet
        """
        {"id": {run_id_int}52001, "name": "Rex", "photoUrls": ["https://example.com/pet.jpg"],
        """
    Then the response should have '400' status code
    When the pet with Id '{run_id_int}52001' is retrieved via GET /pet/{id}
    Then the response should have '404' status code

Scenario: A pet can be created with an empty tags list
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}51001 |
    When a pet is created with the following 'application/json' body via POST /pet
        """
        {"id": {run_id_int}51001, "name": "Rex", "photoUrls": ["https://example.com/pet.jpg"], "tags": []}
        """
    Then the response should have '200' status code
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | {run_id_int}51001           |
        | Name     | Rex                         |
        | PhotoUrl | https://example.com/pet.jpg |

@bug:BUG-003
Scenario Outline: A pet with a missing or empty required field is rejected with 405 Invalid input and not stored
    Given the pets with the following Ids do not exist
        | Id               |
        | {run_id_int}<id> |
    When a pet is created with the following 'application/json' body via POST /pet
        """
        {"id": {run_id_int}<id>, <fields>}
        """
    Then the response should have '405' status code
    When the pet with Id '{run_id_int}<id>' is retrieved via GET /pet/{id}
    Then the response should have '404' status code

    Examples:
        | case              | id    | fields                                                   |
        | name missing      | 30001 | "photoUrls": ["https://example.com/pet.jpg"]             |
        | name empty        | 30002 | "name": "", "photoUrls": ["https://example.com/pet.jpg"] |
        | photoUrls missing | 30003 | "name": "Rex"                                            |
        | photoUrls empty   | 30004 | "name": "Rex", "photoUrls": []                           |
        | both missing      | 30005 | "status": "available"                                    |

@bug:BUG-003
Scenario: A pet with an invalid status is rejected with 405 Invalid input and not stored
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}30011 |
        | {run_id_int}30012 |
        | {run_id_int}30013 |
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}30011           |
        | Name     | Rex                         |
        | Status   | flying                      |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '405' status code
    When the pet with Id '{run_id_int}30011' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}30012           |
        | Name     | Rex                         |
        | Status   | Available                   |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '405' status code
    When the pet with Id '{run_id_int}30012' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}30013           |
        | Name     | Rex                         |
        | Status   |                             |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '405' status code
    When the pet with Id '{run_id_int}30013' is retrieved via GET /pet/{id}
    Then the response should have '404' status code

@bug:BUG-007
Scenario: A pet with a value of the wrong JSON type is rejected with 405 Invalid input and not stored
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}31001 |
        | {run_id_int}31002 |
    When a pet is created with the following 'application/json' body via POST /pet
        """
        {"id": {run_id_int}31001, "name": "Rex", "photoUrls": "https://example.com/pet.jpg"}
        """
    Then the response should have '405' status code
    When the pet with Id '{run_id_int}31001' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    When a pet is created with the following 'application/json' body via POST /pet
        """
        {"id": {run_id_int}31002, "name": "Rex", "photoUrls": ["https://example.com/pet.jpg"], "category": "dogs"}
        """
    Then the response should have '405' status code
    When the pet with Id '{run_id_int}31002' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    When a pet is created with the following 'application/json' body via POST /pet
        """
        {"id": "abc", "name": "Rex", "photoUrls": ["https://example.com/pet.jpg"]}
        """
    Then the response should have '405' status code
    When a pet is created with the following 'application/json' body via POST /pet
        """
        [{"name": "Rex", "photoUrls": ["https://example.com/pet.jpg"]}]
        """
    Then the response should have '405' status code

@bug:BUG-004
Scenario: Getting or deleting a pet by an invalid Id is rejected with 400
    When the pet with Id 'abc' is retrieved via GET /pet/{id}
    Then the response should have '400' status code
    When the pet with Id 'abc' is deleted via DELETE /pet/{id}
    Then the response should have '400' status code
    When the pet with Id '9223372036854775808' is retrieved via GET /pet/{id}
    Then the response should have '400' status code
    When the pet with Id '9223372036854775808' is deleted via DELETE /pet/{id}
    Then the response should have '400' status code
