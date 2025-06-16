using CurrencyConverter.Cache;
using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Models.DTOs.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConverter.Tests.Unit.ProviderTests
{
    public class InMemoryCurrencyHistoricalRateCacheTests
    {
        private readonly IMemoryCache _memoryCache;
        private readonly InMemoryCurrencyHistoricalRateCache _cache;

        public InMemoryCurrencyHistoricalRateCacheTests()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _cache = new InMemoryCurrencyHistoricalRateCache(_memoryCache);
        }

        [Fact]
        public async Task GetAllAsync_WhenEntryExists_ReturnsCachedEntry()
        {
            // Arrange
            var provider = "frankfurter";
            var baseCurrency = "EUR";
            var cacheKey = $"historical:{provider}:{baseCurrency}".ToLowerInvariant();

            var expectedEntry = new HistoricalRateCacheEntry
            {
                LastApiCallUtc = DateTime.UtcNow,
                Rates = new SortedDictionary<DateTime, HistoricalRateDto>
                {
                    [new DateTime(2024, 12, 01)] = new HistoricalRateDto
                    {
                        Date = new DateTime(2024, 12, 01),
                        BaseCurrency = "EUR",
                        Rates = new Dictionary<string, decimal> { ["USD"] = 1.1m }
                    }
                }
            };

            _memoryCache.Set(cacheKey, expectedEntry);

            // Act
            var result = await _cache.GetAllAsync(provider, baseCurrency);

            // Assert
            Assert.Equal(expectedEntry.LastApiCallUtc, result.LastApiCallUtc);
            Assert.Single(result.Rates);
            Assert.Equal(1.1m, result.Rates[new DateTime(2024, 12, 01)].Rates["USD"]);
        }

        [Fact]
        public async Task GetAllAsync_WhenEntryMissing_ReturnsNewEntryAndCachesIt()
        {
            // Arrange
            var provider = "frankfurter";
            var baseCurrency = "EUR";

            // Act
            var result = await _cache.GetAllAsync(provider, baseCurrency);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Rates);
        }

        [Fact]
        public async Task AddToCacheAsync_AddsRatesToNewEntry()
        {
            // Arrange
            var provider = "frankfurter";
            var baseCurrency = "USD";
            var timestamp = DateTime.UtcNow;

            var rates = new List<HistoricalRateDto>
            {
                new HistoricalRateDto
                {
                    Date = new DateTime(2024, 12, 01),
                    BaseCurrency = "USD",
                    Rates = new Dictionary<string, decimal> { ["EUR"] = 0.9m }
                },
                new HistoricalRateDto
                {
                    Date = new DateTime(2024, 12, 02),
                    BaseCurrency = "USD",
                    Rates = new Dictionary<string, decimal> { ["EUR"] = 0.91m }
                }
            };

            // Act
            await _cache.AddToCacheAsync(provider, baseCurrency, rates, timestamp);

            // Assert
            var result = await _cache.GetAllAsync(provider, baseCurrency);
            Assert.Equal(timestamp, result.LastApiCallUtc);
            Assert.Equal(2, result.Rates.Count);
            Assert.Equal(0.9m, result.Rates[new DateTime(2024, 12, 01)].Rates["EUR"]);
            Assert.Equal(0.91m, result.Rates[new DateTime(2024, 12, 02)].Rates["EUR"]);
        }

        [Fact]
        public async Task SetAsync_OverridesExistingCacheEntry()
        {
            // Arrange
            var provider = "frankfurter";
            var baseCurrency = "JPY";

            var newEntry = new HistoricalRateCacheEntry
            {
                LastApiCallUtc = DateTime.UtcNow.AddMinutes(-10),
                Rates = new SortedDictionary<DateTime, HistoricalRateDto>
                {
                    [new DateTime(2024, 11, 01)] = new HistoricalRateDto
                    {
                        Date = new DateTime(2024, 11, 01),
                        BaseCurrency = "JPY",
                        Rates = new Dictionary<string, decimal> { ["USD"] = 0.0067m }
                    }
                }
            };

            // Act
            await _cache.SetAsync(provider, baseCurrency, newEntry);

            // Assert
            var result = await _cache.GetAllAsync(provider, baseCurrency);
            Assert.Equal(newEntry.LastApiCallUtc, result.LastApiCallUtc);
            Assert.Single(result.Rates);
            Assert.Equal(0.0067m, result.Rates[new DateTime(2024, 11, 01)].Rates["USD"]);
        }
    }
}
