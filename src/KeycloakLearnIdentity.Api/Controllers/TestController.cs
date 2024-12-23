using Feree.ResultType.Results;
using KeycloakLearnIdentity.Api.Models;
using KeycloakLearnIdentity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace KeycloakLearnIdentity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly IKeycloakService _keycloakService;

    public TestController(
        IConfiguration configuration,
        IKeycloakService keycloakService)
    {
        _configuration = configuration;
        _httpClient = new HttpClient();
        _keycloakService = keycloakService;
    }

    [HttpGet]
    [Authorize]
    public IActionResult TestGet()
    {
        return Ok("Works");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        /*
        // Keycloak Token URL
        var keycloakSettings = _configuration.GetSection("Authentication:Keycloak");
        var tokenUrl = $"{keycloakSettings["Authority"]}/protocol/openid-connect/token";

        // Prepare the form data for Keycloak
        var formData = new FormUrlEncodedContent(new[]
        {
                new KeyValuePair<string, string>("client_id", keycloakSettings["ClientId"]),
                new KeyValuePair<string, string>("client_secret", keycloakSettings["ClientSecret"]),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", loginRequest.Username),
                new KeyValuePair<string, string>("password", loginRequest.Password),
        });

        // Send the request to Keycloak
        var response = await _httpClient.PostAsync(tokenUrl, formData);

        if (!response.IsSuccessStatusCode)
        {
            // Return error if Keycloak authentication fails
            var errorDetails = await response.Content.ReadAsStringAsync();
            return BadRequest(new { message = "Invalid username or password", details = errorDetails });
        }

        // Parse and return the Keycloak response
        var tokenResponse = await response.Content.ReadAsStringAsync();
        return Ok(JsonSerializer.Deserialize<object>(tokenResponse));
        */
        var tokensResponse = await _keycloakService.Login(loginRequest);

        if (tokensResponse is Failure<TokensResponse> failure)
            return BadRequest(failure.Error.Message);

        if(tokensResponse is Success<TokensResponse> success)
            return Ok(success.Payload);

        return StatusCode(500, "Token response was neither success or failure.");
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshTokens([FromBody] RefreshTokensRequest refreshRequest)
    {
        var tokensResponse = await _keycloakService.RefreshTokens(refreshRequest);

        if (tokensResponse is Failure<TokensResponse> failure)
            return BadRequest(failure.Error.Message);

        if (tokensResponse is Success<TokensResponse> success)
            return Ok(success.Payload);

        return StatusCode(500, "Token response was neither success or failure.");
    }
}
