using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.ExchangeRate.Interfaces;
using CurrencyConverter.Models.DTOs;

namespace CurrencyConverter.Domain
{
    internal class CurrencyExchangeRateService : ICurrencyExchangeRateService
    {
        private readonly IExchangeRateProviderFactory _exchangeRateProviderFactory;

        public CurrencyExchangeRateService(IExchangeRateProviderFactory exchangeRateProviderFactory)
        {
            _exchangeRateProviderFactory = exchangeRateProviderFactory;
        }
        public async Task<LatestRateDto> GetLatestRateAsync(string provider, string baseCurrency)
        {
            return await GetLatestRateHelperAsync(provider, baseCurrency);
        }


        public async Task<CurrencyConversionResult> ConvertAsync(string provider, string baseCurrency, string targetCurrency, decimal amount)
        {
            var latestRate = await GetLatestRateHelperAsync(provider, baseCurrency);

            if (!latestRate.Rates.TryGetValue(targetCurrency, out var rate))
                throw new InvalidOperationException($"Rate for '{targetCurrency}' not available.");

            return new CurrencyConversionResult
            {
                BaseCurrency = baseCurrency,
                TargetCurrency = targetCurrency,
                Amount = amount,
                ConvertedAmount = amount * rate,
                RateTimestamp = latestRate.Timestamp
            };
        }

        private async Task<LatestRateDto> GetLatestRateHelperAsync(string provider, string baseCurrency)
        {
            var exchangeProvider = _exchangeRateProviderFactory.GetProvider(provider);
            return await exchangeProvider.GetLatestRatesAsync(baseCurrency);
        }
    }
}
