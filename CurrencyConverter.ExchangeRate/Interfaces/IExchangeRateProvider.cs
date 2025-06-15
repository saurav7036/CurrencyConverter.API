using CurrencyConverter.Models.DTOs;

namespace CurrencyConverter.ExchangeRate.Interfaces
{
    public interface IExchangeRateProvider
    {
        Task<LatestRateDto> GetLatestRatesAsync(string baseCurrency);
        Task<List<HistoricalRateDto>> GetRatesInRangeAsync(string baseCurrency, DateTime from, DateTime to);
    }
}
