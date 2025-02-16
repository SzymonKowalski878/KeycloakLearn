using Feree.ResultType;
using Feree.ResultType.Results;
using KeycloakLearnIdentity.Api.Models;
using KeycloakLearnIdentity.Api.Services;
using Microsoft.AspNetCore.Authorization;
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

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
    {
        var response = await _keycloakService.Register(registerRequest);

        if (response is Failure failure)
            return BadRequest(failure.Error.Message);

        if (response is Success)
            return Ok();

        return StatusCode(500, "Token response was neither success or failure.");
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var response = await _keycloakService.GetUsers();

        if (response is Failure failure)
            return BadRequest(failure.Error.Message);

        if (response is Success<List<UserResponse>> users)
            return Ok(users.Payload);

        return StatusCode(500, "Token response was neither success or failure.");
    }
}
