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
    /// Behavioral specifications for currency conversion functionality
    /// Tests the complete user journey from request to response
    /// </summary>
    public class CurrencyConversionSpecifications : IClassFixture<TestWebApplicationFactory<Program>>
    {
        private const string ConvertEndpoint = "api/v1/exchange-rates/convert";
        private readonly HttpClient _client;
        private readonly Mock<IHttpClientWrapper> _httpClientWrapperMock;
        private readonly TestDataBuilder _testDataBuilder;

        public CurrencyConversionSpecifications(TestWebApplicationFactory<Program> factory)
        {
            _httpClientWrapperMock = SetupExchangeRateProviderMock(factory);
            _client = factory.CreateClient();
            _testDataBuilder = new TestDataBuilder();
        }

        #region Successful Conversion Scenarios

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

        [Theory(DisplayName = "Given various currency pairs, when converting, then should return correct amounts")]
        [InlineData("GBP", "USD", 50, 62.5)]
        [InlineData("EUR", "GBP", 200, 250)]
        [InlineData("USD", "EUR", 100, 80)]
        public async Task ConvertCurrency_ShouldHandleMultipleCurrencyPairs(
            string fromCurrency, string toCurrency, decimal amount, decimal expectedAmount)
        {
            // Given
            await GivenUserHasConversionPermissions();
            GivenExchangeRateProvider(fromCurrency, toCurrency, expectedAmount / amount);
            var request = GivenConversionRequest(fromCurrency, toCurrency, amount);

            // When
            var response = await WhenRequestingCurrencyConversion(request);

            // Then
            await ThenShouldReturnSuccessfulConversion(response, fromCurrency, toCurrency, amount, expectedAmount);
        }

        #endregion

        #region Authorization Scenarios

        [Fact(DisplayName = "Given user without conversion permission, when requesting conversion, then should return forbidden")]
        public async Task ConvertCurrency_ShouldReturnForbidden_WhenUserLacksPermission()
        {
            // Given
            await GivenUserLacksConversionPermission();
            var request = GivenConversionRequest("EUR", "USD", 100);

            // When
            var response = await WhenRequestingCurrencyConversion(request);

            // Then
            ThenShouldReturnForbidden(response);
        }

        [Fact(DisplayName = "Given no authentication token, when requesting conversion, then should return unauthorized")]
        public async Task ConvertCurrency_ShouldReturnUnauthorized_WhenNoToken()
        {
            // Given - No authentication setup
            var request = GivenConversionRequest("EUR", "USD", 100);

            // When
            var response = await WhenRequestingCurrencyConversion(request);

            // Then
            ThenShouldReturnUnauthorized(response);
        }

        #endregion

        #region Validation Scenarios

        [Fact(DisplayName = "Given same from and to currencies, when converting, then should return bad request")]
        public async Task ConvertCurrency_ShouldReturnBadRequest_WhenSameCurrencies()
        {
            // Given
            await GivenUserHasConversionPermissions();
            var request = GivenConversionRequest("EUR", "EUR", 100);

            // When
            var response = await WhenRequestingCurrencyConversion(request);

            // Then
            await ThenShouldReturnValidationError(response, "cannot be the same");
        }

        [Fact(DisplayName = "Given invalid request payload, when converting, then should return bad request")]
        public async Task ConvertCurrency_ShouldReturnBadRequest_WhenInvalidPayload()
        {
            // Given
            await GivenUserHasConversionPermissions();

            // When
            var response = await WhenRequestingInvalidConversion();

            // Then
            ThenShouldReturnBadRequest(response);
        }

        [Theory(DisplayName = "Given invalid currency codes, when converting, then should return bad request")]
        [InlineData("", "USD")]
        [InlineData("EUR", "")]
        [InlineData(null, "USD")]
        [InlineData("EUR", null)]
        public async Task ConvertCurrency_ShouldReturnBadRequest_WhenInvalidCurrencyCodes(
            string fromCurrency, string toCurrency)
        {
            // Given
            await GivenUserHasConversionPermissions();
            var request = GivenConversionRequest(fromCurrency, toCurrency, 100);

            // When
            var response = await WhenRequestingCurrencyConversion(request);

            // Then
            ThenShouldReturnBadRequest(response);
        }

        #endregion

        #region Given Methods (Test Setup)

        private async Task GivenUserHasConversionPermissions()
        {
            await TestAuthHelper.AddJwtTokenAsync(_client, _testDataBuilder.CreateAuthTokenWith(
                permissions: new Dictionary<string, bool>
                {
                    { "ExchangeRate.ViewLatest", true },
                    { "ExchangeRate.ConvertAmount", true },
                    { "ExchangeRate.ViewHistory", true }
                }));
        }

        private async Task GivenUserLacksConversionPermission()
        {
            await TestAuthHelper.AddJwtTokenAsync(_client, _testDataBuilder.CreateAuthTokenWith(
                permissions: new Dictionary<string, bool>
                {
                    { "ExchangeRate.ViewLatest", true },
                    { "ExchangeRate.ConvertAmount", false }, // Denied
                    { "ExchangeRate.ViewHistory", true }
                }));
        }

        private object GivenConversionRequest(string fromCurrency, string toCurrency, decimal amount, string provider = "frankfurter")
        {
            return _testDataBuilder.CreateConversionRequest(fromCurrency, toCurrency, amount, provider);
        }

        private Mock<IHttpClientWrapper> SetupExchangeRateProviderMock(TestWebApplicationFactory<Program> factory)
        {
            var mock = factory.Mock<IHttpClientWrapper>();
            GivenExchangeRateProvider("EUR", "USD", 1.25m); // Default setup
            return mock;
        }

        private void GivenExchangeRateProvider(string fromCurrency, string toCurrency, decimal rate)
        {
            var responseContent = $"{{\"rates\":{{\"{toCurrency}\":{rate}}}}}";
            _httpClientWrapperMock
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
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

        private async Task<HttpResponseMessage> WhenRequestingInvalidConversion()
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, ConvertEndpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(string.Empty), Encoding.UTF8, "application/json")
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify response content
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CurrencyConversionResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Use FluentAssertions for better readability
            result.Should().NotBeNull();
            result!.BaseCurrency.Should().Be(expectedFromCurrency);
            result.TargetCurrency.Should().Be(expectedToCurrency);
            result.Amount.Should().Be(expectedAmount);
            result.ConvertedAmount.Should().Be(expectedConvertedAmount);
        }

        private void ThenShouldReturnForbidden(HttpResponseMessage response)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        private void ThenShouldReturnUnauthorized(HttpResponseMessage response)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private void ThenShouldReturnBadRequest(HttpResponseMessage response)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private async Task ThenShouldReturnValidationError(HttpResponseMessage response, string expectedErrorMessage)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            
            var errorMessage = await response.Content.ReadAsStringAsync();
            errorMessage.Should().Contain(expectedErrorMessage, 
                because: "validation errors should provide meaningful feedback");
        }

        #endregion
    }
}
