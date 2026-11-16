@pet @http
Feature: Pet HTTP
    The HTTP layer of POST /pet, GET /pet/{id} and DELETE /pet/{id}: the response Content-Type and the Accept
    header (content negotiation).

Scenario: Replies are JSON by default and XML when the Accept header asks for it, until the pet is deleted
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}60001 |
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}60001           |
        | Name     | Pluto                       |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code
    And the response Content-Type should be 'application/json'
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '200' status code
    And the response Content-Type should be 'application/json'
    When the last created pet is retrieved with Accept 'application/xml' via GET /pet/{id}
    Then the response should have '200' status code
    And the response Content-Type should be 'application/xml'
    When the last created pet is deleted via DELETE /pet/{id}
    Then the response should have '200' status code
    And the response Content-Type should be 'application/json'
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    And the response Content-Type should be 'application/json'

@bug:BUG-006
Scenario: Getting a pet in a media type the API does not produce is rejected with 406 Not Acceptable
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}60002 |
    When a pet is created with the following values via POST /pet
        | Field    | Value                       |
        | Id       | {run_id_int}60002           |
        | Name     | Goofy                       |
        | PhotoUrl | https://example.com/pet.jpg |
    Then the response should have '200' status code
    When the last created pet is retrieved with Accept 'text/plain' via GET /pet/{id}
    Then the response should have '406' status code
