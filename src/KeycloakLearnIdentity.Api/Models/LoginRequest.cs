namespace KeycloakLearnIdentity.Api.Models;

public record LoginRequest(
    string Username,
    string Password);