using CurrencyConverter.ExchangeRate.Infrastructure.Http;
using CurrencyConverter.Tests.Common;
using Moq;
using System.Net;
using System.Text;

namespace CurrencyConverter.Tests.Behavioral
{
    public class GetLatestExchangeRateSpecification : IClassFixture<TestWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly Mock<IHttpClientWrapper> _httpClientWrapperMock;
        public GetLatestExchangeRateSpecification(TestWebApplicationFactory<Program> factory)
        {
            // Mock IHttpClientWrapper
            _httpClientWrapperMock = factory.Mock<IHttpClientWrapper>();
            _httpClientWrapperMock
                .Setup(x => x.GetAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"rates\":{\"USD\":1.23}}", Encoding.UTF8, "application/json")
                });

            // Create the test client
            _client = factory.CreateClient();
        }

        [Fact(DisplayName = "Get latest exchange rate for EUR from Frankfurter API")]
        public Task GetLatestExchangeRate_ReturnsRates_ForEUR_FromFrankfurter()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/exchange-rates/latest?BaseCurrency=eur&Provider=frankfurter");
            // Act
            var response = _client.SendAsync(request).Result;
            // Assert
            response.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync().Result;
            Assert.Contains("\"USD\":1.23", content);
            return Task.CompletedTask;
        }
    }
}
