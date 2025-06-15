using Serilog.Context;
using System.Diagnostics;

namespace CurrencyConverter.API.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                // Extract context values from query or headers
                var provider = context.Request.Query["Provider"].FirstOrDefault();
                var baseCurrency = context.Request.Query["BaseCurrency"].FirstOrDefault();
                var clientId = context.User?.FindFirst("clientId")?.Value ?? "anonymous";

                using (LogContext.PushProperty("Provider", provider ?? "unknown"))
                using (LogContext.PushProperty("BaseCurrency", baseCurrency ?? "unknown"))
                using (LogContext.PushProperty("ClientId", clientId))
                {
                    await _next(context);
                }
            }
            finally
            {
                sw.Stop();

                var method = context.Request.Method;
                var path = context.Request.Path;
                var statusCode = context.Response.StatusCode;
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms | IP: {ClientIP} | TraceId: {TraceId}",
                    method, path, statusCode, sw.ElapsedMilliseconds, clientIp, traceId
                );
            }
        }
    }
}
