namespace KeycloakLearnIdentity.Api.Models;

public class User
{
    private User()
    {
    }

    public User(Guid id, string keycloakId, string username, string firstName, string lastName, string email, bool isEnabled, bool isEmailConfirmed, string? confirmationToken)
    {
        Id = id;
        KeycloakId = keycloakId;
        Username = username;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        IsEnabled = isEnabled;
        IsEmailConfirmed = isEmailConfirmed;
        ConfirmationToken = confirmationToken;
    }

    public User(string keycloakId, string username, string firstName, string lastName, string email, bool isEnabled, bool isEmailConfirmed)
        : this(Guid.NewGuid(), keycloakId, username, firstName, lastName, email, isEnabled, isEmailConfirmed, null)
    {
    }

    //for register
    public User(string keycloakId, string username, string firstName, string lastName, string email, bool isEnabled, bool isEmailConfirmed, string? confirmationToken)
        : this(Guid.NewGuid(), keycloakId, username, firstName, lastName, email, isEnabled, isEmailConfirmed, confirmationToken)
    {
    }

    public Guid Id { get; set; }
    public string KeycloakId { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool IsEnabled { get; set; } = default!;
    public bool IsEmailConfirmed { get; set; } = default!;
    public string? ConfirmationToken { get; set; } = default!;

    public User SetIsEnabled(bool? value)
    {
        if (value is not null)
            IsEnabled = value.Value;
        return this;
    }

    public User SetIsEmailConfirmed(bool? value)
    {
        if (value is not null)
            IsEmailConfirmed = value.Value;
        return this;
    }

    public User ResetConfirmationToken()
    {
        ConfirmationToken = null;
        return this;
    }
}