using FixFlow.Application.Exceptions;
using FluentValidation;

namespace FixFlow.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Validation failed.",
                code = "VALIDATION_FAILED",
                details = exception.Errors.Select(x => x.ErrorMessage)
            });
        }
        catch (AppException exception)
        {
            context.Response.StatusCode = exception.StatusCode;
            await context.Response.WriteAsJsonAsync(new { error = exception.Message, code = exception.Code });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            if (environment.IsEnvironment("Testing"))
            {
                await context.Response.WriteAsJsonAsync(new
                {
                    error = exception.Message,
                    code = "INTERNAL_ERROR",
                    type = exception.GetType().Name,
                    detail = exception.ToString()
                });
                return;
            }

            await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred.", code = "INTERNAL_ERROR" });
        }
    }
}
