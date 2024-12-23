using Feree.ResultType.Factories;
using Feree.ResultType.Results;
using KeycloakLearnIdentity.Api.Models;
using System.Text.Json;

namespace KeycloakLearnIdentity.Api.Services;

public interface IKeycloakService
{
    Task<IResult<TokensResponse>> Login(LoginRequest request);
    Task<IResult<TokensResponse>> RefreshTokens(RefreshTokensRequest refreshRequest);
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
}
