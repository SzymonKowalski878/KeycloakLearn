using Feree.ResultType.Results;
using KeycloakLearnIdentity.Api.Models;
using KeycloakLearnIdentity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeycloakLearnIdentity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IKeycloakService _keycloakService;
    private readonly ILogger<TestController> _logger;

    public TestController(IKeycloakService keycloakService, ILogger<TestController> logger)
    {
        _keycloakService = keycloakService;
        _logger = logger;
    }

    [HttpGet("test")]
    [Authorize]
    public IActionResult TestGet()
    {
        return Ok("Works");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        var tokensResponse = await _keycloakService.Login(loginRequest);

        return tokensResponse switch
        {
            Failure<TokensResponse> failure => BadRequest($"Login failed: {failure.Error.Message}"),
            Success<TokensResponse> success => Ok(success.Payload),
            _ => StatusCode(500, "An unexpected error occurred during login.")
        };
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshTokens([FromBody] RefreshTokensRequest refreshRequest)
    {
        var tokensResponse = await _keycloakService.RefreshTokens(refreshRequest);

        return tokensResponse switch
        {
            Failure<TokensResponse> failure => BadRequest($"Token refresh failed: {failure.Error.Message}"),
            Success<TokensResponse> success => Ok(success.Payload),
            _ => StatusCode(500, "An unexpected error occurred during token refresh.")
        };
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
    {
        var response = await _keycloakService.Register(registerRequest);

        return response switch
        {
            Failure failure => BadRequest($"Registration failed: {failure.Error.Message}"),
            Success => Ok(),
            _ => StatusCode(500, "An unexpected error occurred during registration.")
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var response = await _keycloakService.GetUsers();

        return response switch
        {
            Failure<List<UserResponse>> failure => BadRequest($"Failed to retrieve users: {failure.Error.Message}"),
            Success<List<UserResponse>> users => Ok(users.Payload),
            _ => StatusCode(500, "An unexpected error occurred while retrieving users.")
        };
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmEmail(string token)
    {
        var response = await _keycloakService.ConfirmUser(token);

        return response switch
        {
            Failure<User> failure => BadRequest($"Failed to confirm user: {failure.Error.Message}"),
            Success<User> users => Ok(users.Payload),
            _ => StatusCode(500, "An unexpected error occurred while retrieving users.")
        };
    }
}