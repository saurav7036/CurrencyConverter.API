using CurrencyConverter.API.Authorization.Attributes;
using CurrencyConverter.API.Authorization.Filters;
using CurrencyConverter.API.Authorization.Permissions;
using CurrencyConverter.API.ViewModels.Request;
using CurrencyConverter.Domain.Exceptions;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CurrencyConverter.API.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/exchange-rates")]
    [Produces("application/json")]
    public class ExchangeRatesController : ControllerBase
    {
        private static readonly HashSet<string> BlacklistedCurrencies = new()
        {
            "TRY", "PLN", "THB", "MXN"
        };

        private readonly ICurrencyExchangeRateService _currencyExchangeRateService;

        public ExchangeRatesController(ICurrencyExchangeRateService currencyExchangeRateService)
        {
            _currencyExchangeRateService = currencyExchangeRateService;
        }

        [SwaggerOperation(
            Summary = "Get latest exchange rates",
            Description = "Retrieves the latest exchange rates from a provider for a given base currency."
        )]
        [HttpGet("latest")]
        [Authorize]
        [ServiceFilter(typeof(PermissionFilter))]
        [PermissionRequirement(Resource = PermissionResources.ExchangeRate, Action = ExchangeRateActions.ViewLatest)]
        [ProducesResponseType(typeof(LatestRateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<LatestRateDto>> GetLatestRates([FromQuery] CurrencyQueryRequest request)
        {
            request.Normalize();

            var rates = await _currencyExchangeRateService.GetLatestRateAsync(
                provider: request.Provider,
                baseCurrency: request.BaseCurrency);

            return Ok(rates);
        }

        [SwaggerOperation(
            Summary = "Convert currency amount",
            Description = "Converts a specified amount from one currency to another using the given provider."
        )]
        [HttpPost("convert")]
        [Authorize]
        [ServiceFilter(typeof(PermissionFilter))]
        [PermissionRequirement(Resource = PermissionResources.ExchangeRate, Action = ExchangeRateActions.ConvertAmount)]
        [ProducesResponseType(typeof(CurrencyConversionResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CurrencyConversionResult>> ConvertCurrency([FromBody] CurrencyConversionRequest request)
        {
            if (IsBlacklisted(request.FromCurrency) || IsBlacklisted(request.ToCurrency))
            {
                throw new BadRequestException("Conversion involving TRY, PLN, THB, or MXN is not allowed.");
            }

            var result = await _currencyExchangeRateService.ConvertAsync(
                provider: request.Provider,
                baseCurrency: request.FromCurrency.ToUpperInvariant(),
                targetCurrency: request.ToCurrency.ToUpperInvariant(),
                amount: request.AmountInCents);

            return Ok(result);
        }

        [SwaggerOperation(
            Summary = "Get historical exchange rates",
            Description = "Fetches paginated historical exchange rates for a given date range and base currency."
        )]
        [HttpGet("history")]
        [Authorize]
        [ServiceFilter(typeof(PermissionFilter))]
        [PermissionRequirement(Resource = PermissionResources.ExchangeRate, Action = ExchangeRateActions.ViewHistory)]
        [ProducesResponseType(typeof(PagedResult<HistoricalRateDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<HistoricalRateDto>>> GetHistoricalRates([FromQuery] HistoricalRatesQueryRequest request)
        {
            request.Normalize();

            var result = await _currencyExchangeRateService.GetHistoricalRatesAsync(
                provider: request.Provider,
                baseCurrency: request.BaseCurrency,
                from: request.StartDate,
                to: request.EndDate,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize);

            return Ok(result);
        }

        private static bool IsBlacklisted(string currency) =>
            BlacklistedCurrencies.Contains(currency.ToUpperInvariant());
    }
}
