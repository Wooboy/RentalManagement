using Microsoft.AspNetCore.Mvc;

namespace RentalManager.Api.Common;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainValidationException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "請求資料驗證失敗", ex.Message);
        }
        catch (DomainNotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "找不到資源", ex.Message);
        }
        catch (DomainUnauthorizedException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "驗證失敗", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "系統發生錯誤", "系統發生錯誤，請稍後再試");
        }
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string title, string? detail)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = string.IsNullOrWhiteSpace(detail) ? null : detail
        });
    }
}
