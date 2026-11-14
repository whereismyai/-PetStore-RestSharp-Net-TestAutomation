Feature: Pet endpoints
    A pet that the API accepts must be stored exactly as it was sent and returned unchanged until it is
    deleted; invalid input must be rejected without storing anything. Every scenario reads the data back,
    so it checks what was persisted, not only what the response echoed.

Rule: POST /pet stores the pet exactly as it was sent

    Scenario: A pet can be created with all fields and is stored
        When pet is created with the following values via POST /pet
            | Field    | Value                        |
            | Id       | 12345                        |
            | Name     | Baily                        |
            | Status   | available                    |
            | Category | dogs                         |
            | PhotoUrl | https://example.com/dog1.jpg |
            | PhotoUrl | https://example.com/dog2.jpg |
            | Tag      | 1:friendly                   |
            | Tag      | 2:vaccinated                 |
            | Tag      | 3:small                      |
        Then the response should have '200' status code and contain the following values
            | Field     | Value                                                       |
            | Id        | 12345                                                       |
            | Name      | Baily                                                       |
            | Status    | available                                                   |
            | Category  | dogs                                                        |
            | PhotoUrls | https://example.com/dog1.jpg ↕ https://example.com/dog2.jpg |
            | Tags      | 1:friendly ↕ 2:vaccinated ↕ 3:small                         |
        When the pet with ID '12345' is retrieved via GET /pet/{id}
        Then the response should have '200' status code and contain the following values
            | Field     | Value                                                       |
            | Id        | 12345                                                       |
            | Name      | Baily                                                       |
            | Status    | available                                                   |
            | Category  | dogs                                                        |
            | PhotoUrls | https://example.com/dog1.jpg ↕ https://example.com/dog2.jpg |
            | Tags      | 1:friendly ↕ 2:vaccinated ↕ 3:small                         |
        When the pet with ID '12345' is deleted via DELETE /pet/{id}
        Then the response status should be '200'
        And the response message should be '12345'
