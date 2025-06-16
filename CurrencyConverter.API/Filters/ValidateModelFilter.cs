using CurrencyConverter.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CurrencyConverter.API.Filters
{
    public class ValidateModelFilter : IActionFilter
    {
        private readonly ILogger<ValidateModelFilter> _logger;

        public ValidateModelFilter(ILogger<ValidateModelFilter> logger)
        {
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(e => e.Value!.Errors.Count > 0)
                    .SelectMany(e => e.Value!.Errors.Select(x => $"{e.Key}: {x.ErrorMessage}"))
                    .ToArray();

                _logger.LogWarning("Model validation failed: {@Errors}", errors);

                var errorResponse = new ApiErrorResponse
                {
                    StatusCode = 400,
                    ErrorCode = "validation_error",
                    Message = "One or more validation errors occurred.",
                    Details = errors
                };

                context.Result = new JsonResult(errorResponse)
                {
                    StatusCode = 400
                };
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }

}
