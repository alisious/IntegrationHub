namespace IntegrationHub.Api.Middleware
{
    public class ErrorLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorLoggingMiddleware> _logger;

        public ErrorLoggingMiddleware(RequestDelegate next, ILogger<ErrorLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd: {Method} {Path}", context.Request.Method, context.Request.Path);
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Błąd serwera");
            }
        }
    }

}
