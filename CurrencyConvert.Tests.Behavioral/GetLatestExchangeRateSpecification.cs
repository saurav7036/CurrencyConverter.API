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
        private readonly string _apiUrl = "api/v1/exchange-rates/latest?BaseCurrency=eur&Provider=frankfurter";
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
        public async Task GetLatestExchangeRate_ReturnsRates_ForEUR_FromFrankfurter()
        {
            await AddJwtTokenHeader();
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, _apiUrl);
            // Act
            var response = await _client.SendAsync(request);
            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"USD\":1.23", content);
        }

        [Fact(DisplayName = "Returns 401 Unauthorized when token is missing in reuest header")]
        public async Task GetLatestExchangeRate_Forbidden_WhenJwtTokenIsMissingInRequest()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _apiUrl);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "Returns 403 Forbidden when token is expired")]
        public async Task GetLatestExchangeRate_Unauthorized_WhenTokenExpired()
        {
            // Simulate expired token by passing 0s expiration
            await TestAuthHelper.AddJwtTokenAsync(_client, new TestAuthHelper.TestTokenRequest
            {
                Username = "test-user",
                ExpirationInSeconds = 0 // Expired
            });

            var request = new HttpRequestMessage(HttpMethod.Get, _apiUrl);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact(DisplayName = "Returns 403 Forbidden when token is missing permissions claim")]
        public async Task GetLatestExchangeRate_Forbidden_WhenPermissionsMissing()
        {
            await TestAuthHelper.AddJwtTokenAsync(_client, new TestAuthHelper.TestTokenRequest
            {
                Username = "test-user",
                Permissions = null, // No permission claim
                ExpirationInSeconds = 100
            });

            var request = new HttpRequestMessage(HttpMethod.Get, _apiUrl);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact(DisplayName = "Returns 403 Forbidden when user lacks required permission")]
        public async Task GetLatestExchangeRate_Forbidden_WhenPermissionNotGranted()
        {
            await TestAuthHelper.AddJwtTokenAsync(_client, new TestAuthHelper.TestTokenRequest
            {
                Username = "test-user",
                Permissions = new Dictionary<string, bool>
                {
                    { "ExchangeRate.ViewHistory", true },
                    { "ExchangeRate.ViewLatest",false }
                },
                ExpirationInSeconds = 100
            });

            var request = new HttpRequestMessage(HttpMethod.Get, _apiUrl);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
