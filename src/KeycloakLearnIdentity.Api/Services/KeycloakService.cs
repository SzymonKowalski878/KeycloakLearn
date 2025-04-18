﻿using Azure.Core;
using Feree.ResultType;
using Feree.ResultType.Factories;
using Feree.ResultType.Results;
using KeycloakLearnIdentity.Api.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KeycloakLearnIdentity.Api.Services;

public interface IKeycloakService
{
    Task<IResult<TokensResponse>> Login(LoginRequest request);
    Task<IResult<TokensResponse>> RefreshTokens(RefreshTokensRequest refreshRequest);
    Task<IResult<Unit>> Register(RegisterRequest request);
    Task<IResult<List<UserResponse>>> GetUsers();
}

public class KeycloakService : IKeycloakService
{
    private readonly KeycloakSettings _keycloakSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<KeycloakService> _logger;

    public KeycloakService(IOptions<KeycloakSettings> keycloakSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<KeycloakService> logger)
    {
        _keycloakSettings = keycloakSettings.Value;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public async Task<IResult<TokensResponse>> Login(LoginRequest request)
    {
        var tokenUrl = $"{_keycloakSettings.Authority}/protocol/openid-connect/token";

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _keycloakSettings.ClientId),
            new KeyValuePair<string, string>("client_secret", _keycloakSettings.ClientSecret),
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", request.Username),
            new KeyValuePair<string, string>("password", request.Password),
        });

        return await PostFormDataAsync<TokensResponse>(tokenUrl, formData, "Invalid username or password.");
    }

    public async Task<IResult<TokensResponse>> RefreshTokens(RefreshTokensRequest request)
    {
        var tokenUrl = $"{_keycloakSettings.Authority}/protocol/openid-connect/token";

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _keycloakSettings.ClientId),
            new KeyValuePair<string, string>("client_secret", _keycloakSettings.ClientSecret),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", request.RefreshToken)
        });

        return await PostFormDataAsync<TokensResponse>(tokenUrl, formData, "Failed to refresh token.");
    }

    public async Task<IResult<Unit>> Register(RegisterRequest request)
    {
        var tokenResponse = await GetAdminAccessTokenAsync();

        if (string.IsNullOrEmpty(tokenResponse))
        {
            return ResultFactory.CreateFailure("Unable to authenticate with Keycloak.");
        }

        var createUserPayload = new
        {
            username = request.Email,
            email = request.Email,
            firstName = request.FirstName,
            lastName = request.LastName,
            enabled = true,
            credentials = new[]
            {
                new
                {
                    type = "password",
                    value = request.Password,
                    temporary = false
                }
            }
        };

        var keycloakAdminUrl = $"{_keycloakSettings.AdminLink}/users";
        var jsonContent = new StringContent(JsonSerializer.Serialize(createUserPayload), Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, keycloakAdminUrl)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse) },
            Content = jsonContent
        };

        var response = await _httpClient.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to register user. Details: {ErrorDetails}", errorDetails);
            return ResultFactory.CreateFailure($"Failed to register user. Details: {errorDetails}");
        }

        return ResultFactory.CreateSuccess();
    }

    public async Task<IResult<List<UserResponse>>> GetUsers()
    {
        var tokenResponse = await GetAdminAccessTokenAsync();

        if (string.IsNullOrEmpty(tokenResponse))
        {
            return ResultFactory.CreateFailure<List<UserResponse>>("Unable to authenticate with Keycloak.");
        }

        var keycloakAdminUrl = $"{_keycloakSettings.AdminLink}/users";
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, keycloakAdminUrl)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse) }
        };

        var response = await _httpClient.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to retrieve users. Details: {ErrorDetails}", errorDetails);
            return ResultFactory.CreateFailure<List<UserResponse>>($"Failed to retrieve users. Details: {errorDetails}");
        }

        var usersResponse = await response.Content.ReadAsStringAsync();
        var deserializedUsers = JsonSerializer.Deserialize<List<UserResponse>>(usersResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (deserializedUsers is null)
            return ResultFactory.CreateFailure<List<UserResponse>>("Deserialization error.");

        return ResultFactory.CreateSuccess(deserializedUsers);
    }

    private async Task<string?> GetAdminAccessTokenAsync()
    {
        var tokenUrl = $"{_keycloakSettings.MasterAuthority}/protocol/openid-connect/token";

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", "admin-cli"),
            new KeyValuePair<string, string>("client_secret", _keycloakSettings.ClientSecret),
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", _keycloakSettings.AdminUsername),
            new KeyValuePair<string, string>("password", _keycloakSettings.AdminPassword)
        });

        var response = await _httpClient.PostAsync(tokenUrl, formData);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get admin access token.");
            return null;
        }

        var tokenResponse = await response.Content.ReadAsStringAsync();
        var deserializedResponse = JsonSerializer.Deserialize<TokensResponse>(tokenResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return deserializedResponse?.AccessToken;
    }

    private async Task<IResult<T>> PostFormDataAsync<T>(string url, FormUrlEncodedContent formData, string errorMessage)
    {
        var response = await _httpClient.PostAsync(url, formData);

        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
            _logger.LogError("{ErrorMessage} Details: {ErrorDetails}", errorMessage, errorDetails);
            return ResultFactory.CreateFailure<T>($"{errorMessage} Details: {errorDetails}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var deserializedResponse = JsonSerializer.Deserialize<T>(responseContent);

        if (deserializedResponse is null)
        {
            _logger.LogError("Deserialization error.");
            return ResultFactory.CreateFailure<T>("Deserialization error.");
        }

        return ResultFactory.CreateSuccess(deserializedResponse);
    }
}