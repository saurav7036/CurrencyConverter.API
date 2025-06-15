using CurrencyConverter.ExchangeRate.Infrastructure.Http;
using CurrencyConverter.ExchangeRate.Interfaces;
using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Models.DTOs.Frankfurter;
using System.Net.Http.Json;

namespace CurrencyConverter.ExchangeRate
{
    internal class FrankfurterProvider : IExchangeRateProvider
    {
        private readonly IHttpClientWrapper _http;

        public FrankfurterProvider(IHttpClientWrapper http)
        {
            _http = http;
        }

        public async Task<LatestRateDto> GetLatestRatesAsync(string baseCurrency)
        {
            var response = await _http.GetAsync("FrankfurterClient", $"latest?from={baseCurrency}");
            
            response.EnsureSuccessStatusCode();

            var apiResult = await response.Content.ReadFromJsonAsync<FrankfurterLatestApiResponse>();
            if (apiResult == null)
                throw new InvalidOperationException("Deserialization failed");

            return new LatestRateDto
            {
                Timestamp = apiResult.Date,
                BaseCurrency = apiResult.Base,
                Rates = apiResult.Rates
            };
        }
    }
}
