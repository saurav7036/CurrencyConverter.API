using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Models.DTOs.Caching;

namespace CurrencyConverter.Cache.Interfaces
{
    public interface ICurrencyHistoricalRateCache
    {
        Task<HistoricalRateCacheEntry> GetAllAsync(string providerKey, string baseCurrency);
        Task AddToCacheAsync(string providerKey, string baseCurrency, List<HistoricalRateDto> rates, DateTime timestamp);
        Task SetAsync(string providerKey, string baseCurrency, HistoricalRateCacheEntry entry);
    }
}
