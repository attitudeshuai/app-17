namespace MedCabinet.Application.DTOs.Auth;

public class RegisterRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

public class UpdateProfileRequestDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Avatar { get; set; }
}
