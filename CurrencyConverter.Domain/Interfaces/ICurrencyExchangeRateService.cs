using CurrencyConverter.Models.DTOs;

namespace CurrencyConverter.Domain.Interfaces
{
    public interface ICurrencyExchangeRateService
    {
        Task<LatestRateDto> GetLatestRateAsync(string provider, string baseCurrency);
        Task<CurrencyConversionResult> ConvertAsync(
           string provider,
           string baseCurrency,
           string targetCurrency,
           decimal amount);
    }
}
