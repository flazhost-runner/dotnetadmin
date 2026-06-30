Feature: Role-Based Access Control
    In order to secure admin resources
    As an administrator
    I want RBAC to properly gate access

Scenario: Unauthenticated user cannot access admin panel
    Given I am not authenticated
    When I GET "/admin/v1/dashboard" with no redirect
    Then the response status should be 302

Scenario: Unauthenticated user cannot access protected API
    Given I am not authenticated
    When I GET "/api/v1/access/permission"
    Then the response status should be 401
