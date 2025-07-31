using CurrencyConverter.ExchangeRate.Infrastructure.Http;
using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Tests.Common;
using FluentAssertions;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CurrencyConverter.Tests.Behavioral
{
    /// <summary>
    /// Improved behavioral specifications for currency conversion
    /// Demonstrates clean Given-When-Then patterns with FluentAssertions
    /// </summary>
    public class ImprovedCurrencyConversionSpecifications : IClassFixture<TestWebApplicationFactory<Program>>
    {
        private const string ConvertEndpoint = "api/v1/exchange-rates/convert";
        private readonly HttpClient _client;
        private readonly Mock<IHttpClientWrapper> _httpClientWrapperMock;

        public ImprovedCurrencyConversionSpecifications(TestWebApplicationFactory<Program> factory)
        {
            _httpClientWrapperMock = factory.Mock<IHttpClientWrapper>();
            SetupDefaultExchangeRateProvider();
            _client = factory.CreateClient();
        }

        [Fact(DisplayName = "Given valid EUR to USD conversion request, when user has permission, then should return converted amount")]
        public async Task ConvertCurrency_ShouldReturnConvertedAmount_WhenValidRequest()
        {
            // Given
            await GivenUserHasConversionPermissions();
            var request = GivenConversionRequest("EUR", "USD", 100);

            // When
            var response = await WhenRequestingCurrencyConversion(request);

            // Then
            await ThenShouldReturnSuccessfulConversion(response, "EUR", "USD", 100, 125);
        }

        [Fact(DisplayName = "Given user without permission, when requesting conversion, then should return forbidden")]
        public async Task ConvertCurrency_ShouldReturnForbidden_WhenUserLacksPermission()
        {
            // Given
            await GivenUserWithoutConversionPermission();
            var request = GivenConversionRequest("EUR", "USD", 100);

            // When
            var response = await WhenRequestingCurrencyConversion(request);

            // Then
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                because: "users without conversion permission should be denied access");
        }

        [Theory(DisplayName = "Given invalid currency pairs, when converting, then should return bad request")]
        [InlineData("", "USD", "empty from currency should be rejected")]
        [InlineData("EUR", "", "empty to currency should be rejected")]
        [InlineData("EUR", "EUR", "same currencies should be rejected")]
        public async Task ConvertCurrency_ShouldReturnBadRequest_WhenInvalidInput(
            string fromCurrency, string toCurrency, string reason)
        {
            // Given
            await GivenUserHasConversionPermissions();
            var request = GivenConversionRequest(fromCurrency, toCurrency, 100);

            // When
            var response = await WhenRequestingCurrencyConversion(request);

            // Then
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, because: reason);
        }

        #region Given Methods (Test Setup)

        private async Task GivenUserHasConversionPermissions()
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

        private async Task GivenUserWithoutConversionPermission()
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
        }

        private object GivenConversionRequest(string fromCurrency, string toCurrency, decimal amount)
        {
            return new
            {
                Provider = "frankfurter",
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                AmountInCents = amount
            };
        }

        private void SetupDefaultExchangeRateProvider()
        {
            _httpClientWrapperMock
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"rates\":{\"USD\":1.25}}", Encoding.UTF8, "application/json")
                });
        }

        #endregion

        #region When Methods (Actions)

        private async Task<HttpResponseMessage> WhenRequestingCurrencyConversion(object request)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, ConvertEndpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };

            return await _client.SendAsync(httpRequest);
        }

        #endregion

        #region Then Methods (Assertions)

        private async Task ThenShouldReturnSuccessfulConversion(
            HttpResponseMessage response,
            string expectedFromCurrency,
            string expectedToCurrency,
            decimal expectedAmount,
            decimal expectedConvertedAmount)
        {
            // Verify response status
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                because: "valid conversion requests should succeed");

            // Verify response content
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CurrencyConversionResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Use FluentAssertions for better readability and error messages
            result.Should().NotBeNull(because: "response should contain conversion result");
            result!.BaseCurrency.Should().Be(expectedFromCurrency,
                because: "the base currency should match the request");
            result.TargetCurrency.Should().Be(expectedToCurrency,
                because: "the target currency should match the request");
            result.Amount.Should().Be(expectedAmount,
                because: "the amount should match the request");
            result.ConvertedAmount.Should().Be(expectedConvertedAmount,
                because: "the converted amount should be calculated correctly");
        }

        #endregion
    }
}