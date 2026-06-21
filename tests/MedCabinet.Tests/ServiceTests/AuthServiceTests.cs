using FluentAssertions;
using MedCabinet.Application.DTOs.Auth;
using MedCabinet.Application.Interfaces;
using MedCabinet.Application.Services;
using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MedCabinet.Tests.ServiceTests;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        // 配置 JWT 设置
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(x => x["Secret"]).Returns("MedCabinetSuperSecretKeyForJWTToken2024");
        jwtSection.Setup(x => x["Issuer"]).Returns("MedCabinet");
        jwtSection.Setup(x => x["Audience"]).Returns("MedCabinetApi");
        _configurationMock.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);

        _authService = new AuthService(_unitOfWorkMock.Object, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithNewUser_ShouldReturnSuccess()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "password123"
        };

        _unitOfWorkMock.Setup(x => x.Users.ExistsAsync(u => u.Username == request.Username))
            .ReturnsAsync(false);
        _unitOfWorkMock.Setup(x => x.Users.ExistsAsync(u => u.Email == request.Email))
            .ReturnsAsync(false);
        _unitOfWorkMock.Setup(x => x.Users.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Code.Should().Be(200);
        result.Message.Should().Be("注册成功");
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data.User.Username.Should().Be(request.Username);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "existinguser",
            Email = "new@example.com",
            Password = "password123"
        };

        _unitOfWorkMock.Setup(x => x.Users.ExistsAsync(u => u.Username == request.Username))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Code.Should().Be(400);
        result.Message.Should().Be("用户名已存在");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "password123"
        };

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = hashedPassword
        };

        _unitOfWorkMock.Setup(x => x.Users.FindAsync(u => u.Username == request.Username))
            .ReturnsAsync(new List<User> { user });
        _unitOfWorkMock.Setup(x => x.Users.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Code.Should().Be(200);
        result.Message.Should().Be("登录成功");
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data.User.Username.Should().Be(request.Username);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = hashedPassword
        };

        _unitOfWorkMock.Setup(x => x.Users.FindAsync(u => u.Username == request.Username))
            .ReturnsAsync(new List<User> { user });

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Code.Should().Be(401);
        result.Message.Should().Be("用户名或密码错误");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var userId = 1;
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com"
        };

        _unitOfWorkMock.Setup(x => x.Users.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.GetCurrentUserAsync(userId);

        // Assert
        result.Code.Should().Be(200);
        result.Data.Should().NotBeNull();
        result.Data!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var userId = 999;

        _unitOfWorkMock.Setup(x => x.Users.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.GetCurrentUserAsync(userId);

        // Assert
        result.Code.Should().Be(404);
        result.Message.Should().Be("用户不存在");
    }
}
