using MedCabinet.Application.DTOs.Auth;
using MedCabinet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedCabinet.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Application.DTOs.Common.ApiResponse<LoginResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        _logger.LogInformation($"用户注册请求: {request.Username}");
        var response = await _authService.RegisterAsync(request);
        return StatusCode(response.Code, response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Application.DTOs.Common.ApiResponse<LoginResponseDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        _logger.LogInformation($"用户登录请求: {request.Username}");
        var response = await _authService.LoginAsync(request);
        return StatusCode(response.Code, response);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Application.DTOs.Common.ApiResponse<UserDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var response = await _authService.GetCurrentUserAsync(userId);
        return StatusCode(response.Code, response);
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Application.DTOs.Common.ApiResponse<UserDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var response = await _authService.UpdateProfileAsync(userId, request);
        return StatusCode(response.Code, response);
    }
}
