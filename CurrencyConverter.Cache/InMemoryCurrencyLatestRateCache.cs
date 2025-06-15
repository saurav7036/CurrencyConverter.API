using CurrencyConverter.Cache.Interfaces;
using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Models.DTOs.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConverter.Cache
{
    internal class InMemoryCurrencyLatestRateCache : ICurrencyLatestRateCache
    {
        private readonly IMemoryCache _cache;
        public InMemoryCurrencyLatestRateCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<LatestRateCacheEntry?> TryGetAsync(string providerKey, string baseCurrency)
        {
            var key = GetCacheKey(providerKey, baseCurrency);
            _cache.TryGetValue(key, out LatestRateCacheEntry? entry);
            return Task.FromResult(entry);
        }

        public Task SetAsync(string providerKey, string baseCurrency, LatestRateDto rate, DateTime timestamp)
        {
            var key = GetCacheKey(providerKey, baseCurrency);
            var entry = new LatestRateCacheEntry
            {
                Rate = rate,
                LastApiCallUtc = timestamp
            };
            _cache.Set(key, entry);
            return Task.CompletedTask;
        }

        private static string GetCacheKey(string providerKey, string baseCurrency) =>
            $"latest:{providerKey}:{baseCurrency}".ToLowerInvariant();
    }
}
