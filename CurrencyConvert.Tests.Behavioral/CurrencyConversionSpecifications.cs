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
        private const string _requestUri = "api/v1/exchange-rates/convert";
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
            await AddJwtTokenHeader();

            var payload = CreateConversionPayload("EUR", "USD", 100);

            var request = new HttpRequestMessage(HttpMethod.Post, _requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var response = await _client.SendAsync(request);
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

        [Fact(DisplayName = "Returns 403 Forbidden when user lacks ConvertAmount permission")]
        public async Task ConvertCurrency_Forbidden_WhenPermissionNotGranted()
        {
            await TestAuthHelper.AddJwtTokenAsync(_client, new TestAuthHelper.TestTokenRequest
            {
                Username = "test-user",
                Permissions = new Dictionary<string, bool>
                {
                    { "ExchangeRate.ViewLatest", true },
                    { "ExchangeRate.ConvertAmount", false }, // Denied
                    { "ExchangeRate.ViewHistory", true }
                },
                ExpirationInSeconds = 100
            });

            var payload = CreateConversionPayload("EUR", "USD");

            var request = new HttpRequestMessage(HttpMethod.Post, _requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact(DisplayName = "Returns 400 BadRequest when FromCurrency and ToCurrency are the same")]
        public async Task ConvertCurrency_BadRequest_WhenFromAndToCurrenciesAreSame()
        {
            await AddJwtTokenHeader();

            var payload = CreateConversionPayload("EUR", "EUR", 100); // Invalid

            var request = new HttpRequestMessage(HttpMethod.Post, _requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var errorMessage = await response.Content.ReadAsStringAsync();
            Assert.Contains("cannot be the same", errorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact(DisplayName = "Returns 400 BadRequest when required fields are the missing")]
        public async Task ConvertCurrency_BadRequest_WhenRequiredFieldsAreMissing()
        {
            await AddJwtTokenHeader();

            var request = new HttpRequestMessage(HttpMethod.Post, _requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(string.Empty), Encoding.UTF8, "application/json")
            };

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

        private static object CreateConversionPayload(string fromCurrency, string toCurrency, decimal amount = 100, string provider = "frankfurter")
        {
            return new
            {
                Provider = provider,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                AmountInCents = amount
            };
        }
    }
}
