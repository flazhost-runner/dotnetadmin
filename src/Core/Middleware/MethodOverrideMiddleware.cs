namespace DotNetAdmin.Core.Middleware;

public class MethodOverrideMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            var methodOverride = context.Request.Query["_method"].ToString();
            if (!string.IsNullOrEmpty(methodOverride))
            {
                var upper = methodOverride.ToUpperInvariant();
                if (upper is "PUT" or "DELETE" or "PATCH")
                    context.Request.Method = upper;
            }
        }
        await next(context);
    }
}
