using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DotNetAdmin.Core.Middleware;

public class ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment env)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var isApiPath = context.Request.Path.StartsWithSegments("/api");

        if (exception is AppException appEx)
        {
            logger.LogWarning("AppException {Status}: {Message}", appEx.StatusCode, appEx.Message);

            if (isApiPath)
            {
                context.Response.StatusCode = appEx.StatusCode;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = appEx.Message,
                    errors = appEx.Errors
                }, cancellationToken);
            }
            else
            {
                // Web: flash message + redirect
                SetFlash(context, appEx.StatusCode >= 500 ? "error" : "warning", appEx.Message);
                var redirectUrl = appEx.StatusCode == 401
                    ? "/auth/login"
                    : context.Request.Headers.Referer.FirstOrDefault() ?? "/admin/v1/dashboard";
                context.Response.Redirect(redirectUrl);
            }
            return true;
        }

        // Non-AppException → 500
        logger.LogError(exception, "Unhandled exception");

        if (isApiPath)
        {
            context.Response.StatusCode = 500;
            var message = env.IsDevelopment() ? exception.Message : "Internal server error";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message
            }, cancellationToken);
        }
        else
        {
            SetFlash(context, "error", env.IsDevelopment()
                ? $"Error: {exception.Message}"
                : "An unexpected error occurred. Please try again.");
            context.Response.Redirect(context.Request.Headers.Referer.FirstOrDefault() ?? "/admin/v1/dashboard");
        }

        return true;
    }

    private static void SetFlash(HttpContext context, string key, string message)
    {
        context.Session.SetString("flash_key", key);
        context.Session.SetString("flash_message", message);
    }
}
