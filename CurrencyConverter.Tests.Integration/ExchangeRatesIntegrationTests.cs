using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Tests.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;

namespace CurrencyConverter.Tests.Integration
{
    public class ExchangeRatesIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ExchangeRatesIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetLatestRates_ReturnsExpectedResponse()
        {
            // Arrange
            var baseCurrency = "EUR";
            await AddJwtTokenHeader();
            var url = $"/api/v1/exchange-rates/latest?provider=frankfurter&baseCurrency={baseCurrency}";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<LatestRateDto>();
            Assert.NotNull(content);
            Assert.Equal(baseCurrency, content.BaseCurrency);
            Assert.NotEmpty(content.Rates);
        }

        [Fact]
        public async Task ConvertCurrency_ReturnsExpectedConversion()
        {
            // Arrange
            var request = new
            {
                Provider = "frankfurter",
                ToCurrency = "EUR",
                FromCurrency = "USD",
                AmountInCents = 100
            };
            await AddJwtTokenHeader();
            var url = "/api/v1/exchange-rates/convert";

            // Act
            var response = await _client.PostAsJsonAsync(url, request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            // Optionally assert the returned content if needed
        }

        [Fact]
        public async Task GetHistoricalRates_ReturnsPaginatedResults()
        {
            // Arrange
            await AddJwtTokenHeader();
            var startDate = DateTime.UtcNow.Date.AddDays(-2).ToString("yyyy-MM-dd");
            var endDate = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
            var url = $"api/v1/exchange-rates/history?StartDate={startDate}&EndDate={endDate}&PageNumber=1&PageSize=2&BaseCurrency=eur&Provider=frankfurter";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<PagedResult<HistoricalRateDto>>();
            Assert.NotNull(content);
        }

        private async Task AddJwtTokenHeader()
        {
            await TestAuthHelper.AddJwtTokenAsync(_client, new TestAuthHelper.TestTokenRequest
            {
                Username = "test-user",
                Permissions = new Dictionary<string, bool>
                {
                    { "ExchangeRate.ViewLatest", true },
                    { "ExchangeRate.ConvertAmount", true },
                    { "ExchangeRate.ViewHistory", true }
                },
                ExpirationInSeconds = 100
            });
        }
    }
}
