using Azure.Core;
using Feree.ResultType;
using Feree.ResultType.Factories;
using Feree.ResultType.Results;
using KeycloakLearnIdentity.Api.Models;
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
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public KeycloakService(IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<IResult<TokensResponse>> Login(LoginRequest request)
    {
        var keycloakSettings = _configuration.GetSection("Authentication:Keycloak");
        var tokenUrl = $"{keycloakSettings["Authority"]}/protocol/openid-connect/token";

        var formData = new FormUrlEncodedContent(new[]
        {
                new KeyValuePair<string, string>("client_id", keycloakSettings["ClientId"]),
                new KeyValuePair<string, string>("client_secret", keycloakSettings["ClientSecret"]),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", request.Username),
                new KeyValuePair<string, string>("password", request.Password),
        });

        var response = await _httpClient.PostAsync(tokenUrl, formData);

        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
            return ResultFactory.CreateFailure<TokensResponse>($"Invalid username or password. Details: {JsonSerializer.Serialize(errorDetails)}");
        }

        var tokenResponse = await response.Content.ReadAsStringAsync();
        var deserializedTokens = JsonSerializer.Deserialize<TokensResponse>(tokenResponse);

        if (deserializedTokens is null)
            return ResultFactory.CreateFailure<TokensResponse>("Deserialization error");

        return ResultFactory.CreateSuccess(deserializedTokens);
    }

    public async Task<IResult<TokensResponse>> RefreshTokens(RefreshTokensRequest request)
    {
        var keycloakSettings = _configuration.GetSection("Authentication:Keycloak");
        var tokenUrl = $"{keycloakSettings["Authority"]}/protocol/openid-connect/token";

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", keycloakSettings["ClientId"]),
            new KeyValuePair<string, string>("client_secret", keycloakSettings["ClientSecret"]),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", request.RefreshToken)
        });

        var response = await _httpClient.PostAsync(tokenUrl, formData);

        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
            return ResultFactory.CreateFailure<TokensResponse>($"Failed to refresh token. Details: {JsonSerializer.Serialize(errorDetails)}");
        }

        var tokenResponse = await response.Content.ReadAsStringAsync();
        var deserializedTokens = JsonSerializer.Deserialize<TokensResponse>(tokenResponse);

        if (deserializedTokens is null)
            return ResultFactory.CreateFailure<TokensResponse>("Deserialization error.");

        return ResultFactory.CreateSuccess(deserializedTokens);
    }

    public async Task<IResult<Unit>> Register(RegisterRequest request)
    {
        var keycloakSettings = _configuration.GetSection("Authentication:Keycloak");
        
        var tokenResponse = await GetAdminAccessTokenAsync(keycloakSettings);

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

        var keycloakAdminUrl = $"{keycloakSettings["AdminLink"]}/users";
        var jsonContent = new StringContent(JsonSerializer.Serialize(createUserPayload), Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, keycloakAdminUrl);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);
        httpRequest.Content = jsonContent;

        var response = await _httpClient.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
            return ResultFactory.CreateFailure($"Failed to register user. Details: {errorDetails}");
        }

        return ResultFactory.CreateSuccess();
    }

    private async Task<string?> GetAdminAccessTokenAsync(IConfigurationSection keycloakSettings)
    {
        var tokenUrl = $"{keycloakSettings["MasterAuthority"]}/protocol/openid-connect/token";

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", "admin-cli"),
            new KeyValuePair<string, string>("client_secret", keycloakSettings["ClientSecret"]),
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", keycloakSettings["AdminUsername"]),
            new KeyValuePair<string, string>("password", keycloakSettings["AdminPassword"])
        });

        var response = await _httpClient.PostAsync(tokenUrl, formData);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var tokenResponse = await response.Content.ReadAsStringAsync();
        var deserializedResponse = JsonSerializer.Deserialize<TokensResponse>(tokenResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return deserializedResponse?.AccessToken;
    }

    public async Task<IResult<List<UserResponse>>> GetUsers()
    {
        var keycloakSettings = _configuration.GetSection("Authentication:Keycloak");
        var tokenResponse = await GetAdminAccessTokenAsync(keycloakSettings);

        if (string.IsNullOrEmpty(tokenResponse))
        {
            return ResultFactory.CreateFailure<List<UserResponse>>("Unable to authenticate with Keycloak.");
        }

        var keycloakAdminUrl = $"{keycloakSettings["AdminLink"]}/users";
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, keycloakAdminUrl);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);

        var response = await _httpClient.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
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
}
