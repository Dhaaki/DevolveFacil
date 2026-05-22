using System.Text.Json;
using DevolveFacill.Core.Domain;

namespace DevolveFacill.Api.Middleware;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found");
            await WriteError(context, 404, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt");
            await WriteError(context, 403, ex.Message);
        }
        catch (InvalidTransitionException ex)
        {
            logger.LogWarning(ex, "Invalid state transition");
            await WriteError(context, 409, ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error");
            await WriteError(context, 400, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteError(context, 500, "An internal error occurred.");
        }
    }

    private static async Task WriteError(HttpContext ctx, int statusCode, string message)
    {
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
