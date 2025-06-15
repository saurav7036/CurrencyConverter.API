using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Models.DTOs.Caching;

namespace CurrencyConverter.Cache.Interfaces
{
    public interface ICurrencyLatestRateCache
    {
        Task<LatestRateCacheEntry?> TryGetAsync(string providerKey, string baseCurrency);
        Task SetAsync(string providerKey, string baseCurrency, LatestRateDto rate, DateTime timestamp);
    }
}
