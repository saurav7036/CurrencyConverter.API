using CurrencyConverter.ExchangeRate.Infrastructure.Http;
using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Tests.Common;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CurrencyConverter.Tests.Behavioral
{
    public class CurrencyConversionSpecifications : IClassFixture<TestWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly Mock<IHttpClientWrapper> _httpClientWrapperMock;

        public CurrencyConversionSpecifications(TestWebApplicationFactory<Program> factory)
        {
            _httpClientWrapperMock = factory.Mock<IHttpClientWrapper>();

            _httpClientWrapperMock
                .Setup(x => x.GetAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"rates\":{\"USD\":1.25}}", Encoding.UTF8, "application/json")
                });

            _client = factory.CreateClient();
        }

        [Fact(DisplayName = "Convert 100 EUR to USD using Frankfurter and assert ConvertedAmount")]
        public async Task TestConvertCurrency()
        {
            // Arrange
            var payload = new
            {
                Provider = "frankfurter",
                FromCurrency = "EUR",
                ToCurrency = "USD",
                Amount = 100
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/exchange-rates/convert")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<CurrencyConversionResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(result);
            Assert.Equal("EUR", result!.BaseCurrency);
            Assert.Equal("USD", result.TargetCurrency);
            Assert.Equal(100, result.Amount);
            Assert.Equal(125, result.ConvertedAmount);
        }
    }
}
