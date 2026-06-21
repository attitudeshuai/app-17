using MedCabinet.Application.DTOs.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace MedCabinet.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发生未处理的异常");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var response = context.Response;

        var apiResponse = ApiResponse.Error("服务器内部错误", (int)HttpStatusCode.InternalServerError);

        switch (exception)
        {
            case UnauthorizedAccessException:
                apiResponse.Code = (int)HttpStatusCode.Unauthorized;
                apiResponse.Message = "未授权访问";
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;
            case KeyNotFoundException:
            case FileNotFoundException:
                apiResponse.Code = (int)HttpStatusCode.NotFound;
                apiResponse.Message = "资源不存在";
                response.StatusCode = (int)HttpStatusCode.NotFound;
                break;
            case ArgumentException:
            case InvalidOperationException:
                apiResponse.Code = (int)HttpStatusCode.BadRequest;
                apiResponse.Message = exception.Message;
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;
            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        var result = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(result);
    }
}
