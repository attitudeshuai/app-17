using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Mapster;
using MedCabinet.Application.DTOs.Auth;
using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.Interfaces;
using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MedCabinet.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        try
        {
            // 检查用户名是否已存在
            if (await _unitOfWork.Users.ExistsAsync(u => u.Username == request.Username))
            {
                return ApiResponse<LoginResponseDto>.Error("用户名已存在");
            }

            // 检查邮箱是否已存在
            if (await _unitOfWork.Users.ExistsAsync(u => u.Email == request.Email))
            {
                return ApiResponse<LoginResponseDto>.Error("邮箱已被注册");
            }

            // 创建用户
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Avatar = request.Avatar,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"用户注册成功: {user.Username}");

            // 生成 token
            var token = GenerateJwtToken(user);
            var response = new LoginResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.Now.AddHours(24),
                User = user.Adapt<UserDto>()
            };

            return ApiResponse<LoginResponseDto>.Success(response, "注册成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户注册失败");
            return ApiResponse<LoginResponseDto>.Error("注册失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        try
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.Username == request.Username);
            var user = users.FirstOrDefault();

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return ApiResponse<LoginResponseDto>.Error("用户名或密码错误", 401);
            }

            user.UpdatedAt = DateTime.Now;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"用户登录成功: {user.Username}");

            var token = GenerateJwtToken(user);
            var response = new LoginResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.Now.AddHours(24),
                User = user.Adapt<UserDto>()
            };

            return ApiResponse<LoginResponseDto>.Success(response, "登录成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户登录失败");
            return ApiResponse<LoginResponseDto>.Error("登录失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<UserDto>> GetCurrentUserAsync(int userId)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserDto>.Error("用户不存在", 404);
            }

            return ApiResponse<UserDto>.Success(user.Adapt<UserDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户信息失败");
            return ApiResponse<UserDto>.Error("获取用户信息失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<UserDto>> UpdateProfileAsync(int userId, UpdateProfileRequestDto request)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserDto>.Error("用户不存在", 404);
            }

            // 检查用户名是否冲突
            if (!string.IsNullOrEmpty(request.Username) && request.Username != user.Username)
            {
                if (await _unitOfWork.Users.ExistsAsync(u => u.Username == request.Username && u.Id != userId))
                {
                    return ApiResponse<UserDto>.Error("用户名已存在");
                }
                user.Username = request.Username;
            }

            // 检查邮箱是否冲突
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                if (await _unitOfWork.Users.ExistsAsync(u => u.Email == request.Email && u.Id != userId))
                {
                    return ApiResponse<UserDto>.Error("邮箱已被注册");
                }
                user.Email = request.Email;
            }

            if (request.Avatar != null)
            {
                user.Avatar = request.Avatar;
            }

            user.UpdatedAt = DateTime.Now;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"用户信息更新成功: {user.Username}");

            return ApiResponse<UserDto>.Success(user.Adapt<UserDto>(), "更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户信息失败");
            return ApiResponse<UserDto>.Error("更新失败: " + ex.Message, 500);
        }
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Secret"] ?? "MedCabinetSuperSecretKeyForJWTToken2024";
        var issuer = jwtSettings["Issuer"] ?? "MedCabinet";
        var audience = jwtSettings["Audience"] ?? "MedCabinetApi";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
