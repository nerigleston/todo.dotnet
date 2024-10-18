public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public AuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (!context.Response.HasStarted)
        {
            if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    statusCode = StatusCodes.Status403Forbidden,
                    message = "You are not authorized to access this resource"
                };

                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
            }

            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    statusCode = StatusCodes.Status401Unauthorized,
                    message = "You are not authenticated"
                };

                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
            }
        }
    }
}
