using CurrencyConverter.ExchangeRate.Infrastructure.Http;
using CurrencyConverter.ExchangeRate.Interfaces;
using CurrencyConverter.Models.Constants;
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
            var response = await _http.GetAsync(HttpClientNames.FrankfurterClient, $"latest?from={baseCurrency}");

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

        public async Task<List<HistoricalRateDto>> GetRatesInRangeAsync(string baseCurrency, DateTime from, DateTime to)
        {
            var url1 = $"?from={baseCurrency}&start_date={from:yyyy-MM-dd}&end_date={to:yyyy-MM-dd}";
            var url = $"{from:yyyy-MM-dd}..{to:yyyy-MM-dd}?symbol={baseCurrency}";
            
            var response = await _http.GetAsync(HttpClientNames.FrankfurterClient, url);
            
           response.EnsureSuccessStatusCode();

            var apiResult = await response.Content.ReadFromJsonAsync<FrankfurterHistoricalApiResponse>();
            if (apiResult == null)
                throw new InvalidOperationException("Deserialization failed");

            return apiResult.RatesByDate
                .Select(kvp => new HistoricalRateDto
                {
                    Date = DateTime.Parse(kvp.Key),
                    BaseCurrency = apiResult.Base,
                    Rates = kvp.Value
                })
                .ToList();
        }
    }
}
