namespace DotNetAdmin.Core.Middleware;

/// <summary>
/// Allows the CSRF token to be passed via ?_csrf= query parameter in addition to
/// the X-CSRF-TOKEN header and _csrf form field (NodeAdmin standard).
/// Runs before antiforgery validation by copying the query param into the request header.
/// </summary>
public class CsrfQueryMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-CSRF-TOKEN";
    private const string QueryParam = "_csrf";

    public async Task InvokeAsync(HttpContext context)
    {
        // Only mutate on state-changing verbs; skip GET/HEAD/OPTIONS
        var method = context.Request.Method;
        if (!HttpMethods.IsGet(method) && !HttpMethods.IsHead(method) && !HttpMethods.IsOptions(method))
        {
            // If header is absent but ?_csrf= is present, promote it to the header
            if (!context.Request.Headers.ContainsKey(HeaderName))
            {
                var token = context.Request.Query[QueryParam].FirstOrDefault();
                if (!string.IsNullOrEmpty(token))
                    context.Request.Headers[HeaderName] = token;
            }
        }

        await next(context);
    }
}
