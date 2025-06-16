using CurrencyConverter.Cache.Interfaces;
using CurrencyConverter.Domain;
using CurrencyConverter.Domain.Exceptions;
using CurrencyConverter.ExchangeRate.Interfaces;
using CurrencyConverter.Models.Configurations;
using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Models.DTOs.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CurrencyConverter.Tests.Unit
{
    public class CurrencyExchangeRateServiceTests
    {
        private readonly Mock<IExchangeRateProviderFactory> _providerFactoryMock = new();
        private readonly Mock<ICurrencyLatestRateCache> _latestCacheMock = new();
        private readonly Mock<ICurrencyHistoricalRateCache> _historicalCacheMock = new();
        private readonly Mock<IExchangeRateProvider> _providerMock = new();
        private readonly Mock<ILogger<CurrencyExchangeRateService>> _loggerMock = new();

        private readonly CurrencyExchangeRateService _service;

        public CurrencyExchangeRateServiceTests()
        {
            var settings = Options.Create(new ExchangeRateSettings
            {
                Providers = new List<ExchangeRateProviderOptions>
                {
                    new ExchangeRateProviderOptions
                    {
                        Name = "test-provider",
                        LatestRateTtlSeconds = 60,
                    }
                }
            });

            _providerFactoryMock.Setup(x => x.GetProvider("test-provider")).Returns(_providerMock.Object);

            _service = new CurrencyExchangeRateService(
                _providerFactoryMock.Object,
                _latestCacheMock.Object,
                _historicalCacheMock.Object,
                settings,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GetLatestRateAsync_ReturnsFromCache_WhenNotExpired()
        {
            var now = DateTime.UtcNow;
            var rateDto = new LatestRateDto { BaseCurrency = "EUR", Timestamp = now, Rates = new Dictionary<string, decimal> { { "USD", 1.1m } } };

            _latestCacheMock.Setup(x => x.TryGetAsync("test-provider", "EUR"))
                .ReturnsAsync(new LatestRateCacheEntry { Rate = rateDto, LastApiCallUtc = now });

            var result = await _service.GetLatestRateAsync("test-provider", "EUR");

            Assert.Equal("EUR", result.BaseCurrency);
            _providerMock.Verify(p => p.GetLatestRatesAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetLatestRateAsync_FetchesFresh_WhenCacheExpired()
        {
            var now = DateTime.UtcNow;
            var expiredTime = now - TimeSpan.FromMinutes(2);
            var rateDto = new LatestRateDto { BaseCurrency = "EUR", Timestamp = now, Rates = new Dictionary<string, decimal> { { "USD", 1.1m } } };

            _latestCacheMock.Setup(x => x.TryGetAsync("test-provider", "EUR"))
                .ReturnsAsync(new LatestRateCacheEntry { Rate = rateDto, LastApiCallUtc = expiredTime });

            _providerMock.Setup(p => p.GetLatestRatesAsync("EUR")).ReturnsAsync(rateDto);

            var result = await _service.GetLatestRateAsync("test-provider", "EUR");

            Assert.Equal("EUR", result.BaseCurrency);
            _providerMock.Verify(p => p.GetLatestRatesAsync("EUR"), Times.Once);
            _latestCacheMock.Verify(x => x.SetAsync("test-provider", "EUR", rateDto, It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task ConvertAsync_ThrowsBadRequest_WhenSameCurrency()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.ConvertAsync("test-provider", "EUR", "EUR", 100));
        }

        [Fact]
        public async Task ConvertAsync_ThrowsBadRequest_WhenTargetCurrencyNotFound()
        {
            var now = DateTime.UtcNow;
            var rateDto = new LatestRateDto { BaseCurrency = "EUR", Timestamp = now, Rates = new Dictionary<string, decimal>() };

            _latestCacheMock.Setup(x => x.TryGetAsync("test-provider", "EUR"))
                .ReturnsAsync(new LatestRateCacheEntry { Rate = rateDto, LastApiCallUtc = now });

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.ConvertAsync("test-provider", "EUR", "USD", 100));
        }

        [Fact]
        public async Task ConvertAsync_ReturnsConvertedAmount_WhenValid()
        {
            var now = DateTime.UtcNow;
            var rateDto = new LatestRateDto
            {
                BaseCurrency = "EUR",
                Timestamp = now,
                Rates = new Dictionary<string, decimal> { { "USD", 1.2m } }
            };

            _latestCacheMock.Setup(x => x.TryGetAsync("test-provider", "EUR"))
                .ReturnsAsync(new LatestRateCacheEntry { Rate = rateDto, LastApiCallUtc = now });

            var result = await _service.ConvertAsync("test-provider", "EUR", "USD", 100);

            Assert.Equal("EUR", result.BaseCurrency);
            Assert.Equal("USD", result.TargetCurrency);
            Assert.Equal(100, result.Amount);
            Assert.Equal(120, result.ConvertedAmount);
        }
    }
}