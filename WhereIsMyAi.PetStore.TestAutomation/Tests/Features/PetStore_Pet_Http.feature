@pet @http
Feature: Pet HTTP
    The HTTP layer of POST /pet, GET /pet/{id} and DELETE /pet/{id}: the response Content-Type, the Accept
    header (content negotiation on GET and POST), the request Content-Type (JSON, XML, unsupported) and the api_key header.

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
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | {run_id_int}60001           |
        | Name     | Pluto                       |
        | PhotoUrl | https://example.com/pet.jpg |
    And the response Content-Type should be 'application/json'
    When the last created pet is retrieved with Accept 'application/xml' via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly this XML
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?><Pet><id>{run_id_int}60001</id><name>Pluto</name><photoUrls><photoUrl>https://example.com/pet.jpg</photoUrl></photoUrls><tags/></Pet>
        """
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
    When the last created pet is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | {run_id_int}60002           |
        | Name     | Goofy                       |
        | PhotoUrl | https://example.com/pet.jpg |

Scenario: A pet is accepted as XML with an XML reply when Accept asks for it, rejected as plain text, and deleted with the api_key header
    Given the pets with the following Ids do not exist
        | Id                |
        | {run_id_int}60003 |
        | {run_id_int}60004 |
    When a pet is created with the following 'text/plain' body via POST /pet
        """
        {"id": {run_id_int}60004, "name": "Odie", "photoUrls": ["https://example.com/pet.jpg"]}
        """
    Then the response should have '415' status code
    When the pet with Id '{run_id_int}60004' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
    When a pet is created with the following 'application/xml' body and Accept 'application/xml' via POST /pet
        """
        <Pet><id>{run_id_int}60003</id><name>Snoopy</name><photoUrls><photoUrl>https://example.com/pet.jpg</photoUrl></photoUrls></Pet>
        """
    Then the response should have '200' status code and the returned pet should be exactly this XML
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?><Pet><id>{run_id_int}60003</id><name>Snoopy</name><photoUrls><photoUrl>https://example.com/pet.jpg</photoUrl></photoUrls><tags/></Pet>
        """
    And the response Content-Type should be 'application/xml'
    When the pet with Id '{run_id_int}60003' is retrieved via GET /pet/{id}
    Then the response should have '200' status code and the returned pet should be exactly
        | Field    | Value                       |
        | Id       | {run_id_int}60003           |
        | Name     | Snoopy                      |
        | PhotoUrl | https://example.com/pet.jpg |
    When the pet with Id '{run_id_int}60003' is deleted with api_key 'special-key' via DELETE /pet/{id}
    Then the response should have '200' status code
    When the pet with Id '{run_id_int}60003' is retrieved via GET /pet/{id}
    Then the response should have '404' status code
