using CurrencyConverter.Domain.Exceptions;

namespace CurrencyConverter.API.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            catch (ServiceException ex)
            {
                _logger.LogWarning(ex, "Service exception: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
                await WriteErrorResponseAsync(context, ex.StatusCode, ex.ErrorCode, ex.Message);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Downstream service unavailable: {Message}", ex.Message);
                await WriteErrorResponseAsync(context, StatusCodes.Status503ServiceUnavailable, "service_unavailable", "Downstream service unavailable:");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred.");
                await WriteErrorResponseAsync(context, StatusCodes.Status500InternalServerError, "internal_error", "An unexpected error occurred.");
            }
        }

        private static async Task WriteErrorResponseAsync(HttpContext context, int statusCode, string errorCode, string message)
        {
            var response = new ApiErrorResponse
            {
                StatusCode = statusCode,
                ErrorCode = errorCode,
                Message = message
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
