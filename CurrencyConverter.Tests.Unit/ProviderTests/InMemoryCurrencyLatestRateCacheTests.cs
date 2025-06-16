using CurrencyConverter.Cache;
using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Models.DTOs.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConverter.Tests.Unit.ProviderTests
{
    public class InMemoryCurrencyLatestRateCacheTests
    {
        private readonly IMemoryCache _memoryCache;
        private readonly InMemoryCurrencyLatestRateCache _cache;

        public InMemoryCurrencyLatestRateCacheTests()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _cache = new InMemoryCurrencyLatestRateCache(_memoryCache);
        }

        [Fact]
        public async Task TryGetAsync_WhenEntryExists_ReturnsEntry()
        {
            // Arrange
            var provider = "frankfurter";
            var baseCurrency = "EUR";
            var cacheKey = $"latest:{provider}:{baseCurrency}".ToLowerInvariant();
            var timestamp = DateTime.UtcNow;

            var expectedEntry = new LatestRateCacheEntry
            {
                LastApiCallUtc = timestamp,
                Rate = new LatestRateDto
                {
                    BaseCurrency = "EUR",
                    Rates = new Dictionary<string, decimal> { ["USD"] = 1.1m }
                }
            };

            _memoryCache.Set(cacheKey, expectedEntry);

            // Act
            var result = await _cache.TryGetAsync(provider, baseCurrency);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("EUR", result!.Rate.BaseCurrency);
            Assert.Equal(1.1m, result.Rate.Rates["USD"]);
            Assert.Equal(timestamp, result.LastApiCallUtc);
        }

        [Fact]
        public async Task TryGetAsync_WhenEntryMissing_ReturnsNull()
        {
            // Arrange
            var provider = "nonexistent";
            var baseCurrency = "XYZ";

            // Act
            var result = await _cache.TryGetAsync(provider, baseCurrency);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetAsync_SavesEntryInCache()
        {
            // Arrange
            var provider = "ecb";
            var baseCurrency = "GBP";
            var timestamp = DateTime.UtcNow;

            var rate = new LatestRateDto
            {
                BaseCurrency = "GBP",
                Rates = new Dictionary<string, decimal> { ["INR"] = 103.56m }
            };

            // Act
            await _cache.SetAsync(provider, baseCurrency, rate, timestamp);

            // Assert
            var result = await _cache.TryGetAsync(provider, baseCurrency);
            Assert.NotNull(result);
            Assert.Equal("GBP", result!.Rate.BaseCurrency);
            Assert.Equal(103.56m, result.Rate.Rates["INR"]);
            Assert.Equal(timestamp, result.LastApiCallUtc);
        }
    }
}
