using CurrencyConverter.Cache.Interfaces;
using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Models.DTOs.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConverter.Cache
{
    internal class InMemoryCurrencyHistoricalRateCache : ICurrencyHistoricalRateCache
    {
        private readonly IMemoryCache _cache;

        public InMemoryCurrencyHistoricalRateCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<HistoricalRateCacheEntry> GetAllAsync(string providerKey, string baseCurrency)
        {
            var key = GetCacheKey(providerKey, baseCurrency);
            if (_cache.TryGetValue(key, out HistoricalRateCacheEntry? entry))
            {
                return Task.FromResult(entry!);
            }

            var newEntry = new HistoricalRateCacheEntry();
            _cache.Set(key, newEntry);
            return Task.FromResult(newEntry);
        }

        public Task AddToCacheAsync(string providerKey, string baseCurrency, List<HistoricalRateDto> rates, DateTime timestamp)
        {
            var key = GetCacheKey(providerKey, baseCurrency);

            if (!_cache.TryGetValue(key, out HistoricalRateCacheEntry? entry))
            {
                entry = new HistoricalRateCacheEntry();
            }

            foreach (var rate in rates)
            {
                entry!.Rates[rate.Date] = rate;
            }

            entry!.LastApiCallUtc = timestamp;
            _cache.Set(key, entry);

            return Task.CompletedTask;
        }

        public Task SetAsync(string providerKey, string baseCurrency, HistoricalRateCacheEntry entry)
        {
            var key = GetCacheKey(providerKey, baseCurrency);
            _cache.Set(key, entry);
            return Task.CompletedTask;
        }

        private static string GetCacheKey(string providerKey, string baseCurrency) =>
            $"historical:{providerKey}:{baseCurrency}".ToLowerInvariant();

    }
}
