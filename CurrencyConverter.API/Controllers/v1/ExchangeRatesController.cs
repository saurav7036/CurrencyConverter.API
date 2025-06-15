using CurrencyConverter.API.ViewModels.Request;
using CurrencyConverter.Domain.Exceptions;
using CurrencyConverter.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.API.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/exchange-rates")]
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

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestRates([FromQuery] CurrencyQueryRequest request)
        {
            request.Normalize();

            var rates = await _currencyExchangeRateService.GetLatestRateAsync(
                provider: request.Provider,
                baseCurrency: request.BaseCurrency);

            return Ok(rates);
        }

        [HttpPost("convert")]
        public async Task<IActionResult> ConvertCurrency([FromBody] CurrencyConversionRequest request)
        {
            if (IsBlacklisted(request.FromCurrency) || IsBlacklisted(request.ToCurrency))
            {
                throw new BadRequestException("Conversion involving TRY, PLN, THB, or MXN is not allowed.");
            }

            var result = await _currencyExchangeRateService.ConvertAsync(
                provider: request.Provider,
                baseCurrency: request.FromCurrency.ToUpperInvariant(),
                targetCurrency: request.ToCurrency.ToUpperInvariant(),
                amount: request.Amount);

            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistoricalRates([FromQuery] HistoricalRatesQueryRequest request)
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
