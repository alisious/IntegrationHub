using IntegrationHub.Common.Exceptions;
using System.Net;

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
            catch (SrpSoapException ex)
            {
                _logger.LogError(ex, "SRP error: {Method} {Path} [{ReqId}]",
                    context.Request.Method, context.Request.Path, ex.RequestId);

                context.Response.StatusCode = (int)HttpStatusCode.BadGateway; // 502
                context.Response.ContentType = "application/problem+json";

                var problem = new
                {
                    type = "https://httpstatuses.io/502",
                    title = "Błąd komunikacji z usługą SRP",
                    status = 502,
                    detail = ex.Message,
                    traceId = context.TraceIdentifier,
                    requestId = ex.RequestId,
                    endpoint = ex.Endpoint,
                    action = ex.Action
                };
                await context.Response.WriteAsJsonAsync(problem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd: {Method} {Path}", context.Request.Method, context.Request.Path);
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "about:blank",
                    title = "Błąd serwera",
                    status = 500,
                    traceId = context.TraceIdentifier
                });
            }
        }
    }

}
