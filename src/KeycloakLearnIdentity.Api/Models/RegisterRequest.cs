﻿namespace KeycloakLearnIdentity.Api.Models;

public record RegisterRequest(string Email, string Password, string FirstName, string LastName);