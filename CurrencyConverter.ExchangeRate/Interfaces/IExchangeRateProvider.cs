using CurrencyConverter.Models.DTOs;

namespace CurrencyConverter.ExchangeRate.Interfaces
{
    public interface IExchangeRateProvider
    {
        Task<LatestRateDto> GetLatestRatesAsync(string baseCurrency);
    }
}
