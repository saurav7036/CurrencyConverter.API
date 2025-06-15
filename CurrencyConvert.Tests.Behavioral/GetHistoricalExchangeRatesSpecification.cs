using CurrencyConverter.ExchangeRate.Infrastructure.Http;
using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Tests.Common;
using CurrencyConverter.Tests.Common.Models;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CurrencyConverter.Tests.Behavioral
{
    public class GetHistoricalExchangeRatesSpecification : IClassFixture<TestWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly Mock<IHttpClientWrapper> _httpClientWrapperMock;

        public GetHistoricalExchangeRatesSpecification(TestWebApplicationFactory<Program> factory)
        {
            _httpClientWrapperMock = factory.Mock<IHttpClientWrapper>();

            var date1 = DateTime.UtcNow.Date.AddDays(-2).ToString("yyyy-MM-dd");
            var date2 = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");

            var response = new TestFrankfurterHistoricalApiResponse
            {
                Base = "EUR",
                StartDate = DateTime.Parse(date1),
                EndDate = DateTime.Parse(date2),
                RatesByDate = new Dictionary<string, Dictionary<string, decimal>>
                {
                    { date1, new Dictionary<string, decimal> { { "USD", 1.10m } } }
                }
            };

            var json = JsonSerializer.Serialize(response);

            _httpClientWrapperMock
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            _client = factory.CreateClient();
        }

        [Fact(DisplayName = "Get historical exchange rates for EUR from Frankfurter with dynamic dates")]
        public async Task TestGetHistoricalRates()
        {
            // Arrange
            var startDate = DateTime.UtcNow.Date.AddDays(-2).ToString("yyyy-MM-dd");
            var endDate = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
            var requestUri = $"api/v1/exchange-rates/history?StartDate={startDate}&EndDate={endDate}&PageNumber=1&PageSize=2&BaseCurrency=eur&Provider=frankfurter";

            // Act
            var response = await _client.GetAsync(requestUri);

            // Assert
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<PagedResult<HistoricalRateDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(result);
            Assert.Equal(1, result!.TotalCount);
            Assert.All(result.Items, item =>
            {
                Assert.Equal("USD", item.Rates.Keys.First());
                Assert.True(item.Rates.Values.First() is 1.10m);
            });
        }
    }
}
