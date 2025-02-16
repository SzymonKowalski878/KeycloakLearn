namespace KeycloakLearnIdentity.Api.Models;

public class UserResponse
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public bool Enabled { get; set; }
}