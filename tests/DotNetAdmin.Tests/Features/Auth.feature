Feature: Authentication
    In order to access the admin panel
    As a registered user
    I want to be able to log in and out securely

Scenario: Successful login with valid credentials
    Given I am on the login page
    When I submit email "admin@admin.com" and password "12345678"
    Then I should receive a JWT token
    And the token should have 3 parts

Scenario: Failed login with wrong password
    Given I am on the login page
    When I submit email "admin@admin.com" and password "WrongPassword!"
    Then the response status should be 401

Scenario: Protected API endpoint requires authentication
    Given I am not authenticated
    When I GET "/api/v1/access/user"
    Then the response status should be 401

Scenario: Protected API endpoint allows authenticated user
    Given I am logged in as "admin@admin.com" with password "12345678"
    When I GET "/api/v1/access/role"
    Then the response status should be 200
