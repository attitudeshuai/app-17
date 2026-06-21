using MedCabinet.Application.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedCabinet.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    protected int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("用户未登录");
        }
        return userId;
    }

    protected IActionResult ApiResult<T>(ApiResponse<T> response)
    {
        return StatusCode(response.Code, response);
    }

    protected IActionResult ApiResult(ApiResponse response)
    {
        return StatusCode(response.Code, response);
    }
}
