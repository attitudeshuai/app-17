using MedCabinet.Application.DTOs.Auth;
using MedCabinet.Application.DTOs.Common;

namespace MedCabinet.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResponse<UserDto>> GetCurrentUserAsync(int userId);
    Task<ApiResponse<UserDto>> UpdateProfileAsync(int userId, UpdateProfileRequestDto request);
}
