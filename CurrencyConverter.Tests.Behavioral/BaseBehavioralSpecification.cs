using CurrencyConverter.Tests.Common;
using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CurrencyConverter.Tests.Behavioral
{
    /// <summary>
    /// Base class for behavioral specifications providing common Given-When-Then patterns
    /// </summary>
    public abstract class BaseBehavioralSpecification : IClassFixture<TestWebApplicationFactory<Program>>
    {
        protected readonly HttpClient Client;
        protected readonly TestWebApplicationFactory<Program> Factory;
        protected readonly TestDataBuilder TestDataBuilder;

        protected BaseBehavioralSpecification(TestWebApplicationFactory<Program> factory)
        {
            Factory = factory;
            Client = factory.CreateClient();
            TestDataBuilder = new TestDataBuilder();
        }

        #region Common Given Methods

        protected async Task GivenAuthenticatedUser(Dictionary<string, bool>? permissions = null)
        {
            var tokenRequest = TestDataBuilder.CreateAuthTokenWith(permissions: permissions ?? GetDefaultPermissions());
            await TestAuthHelper.AddJwtTokenAsync(Client, tokenRequest);
        }

        protected virtual Dictionary<string, bool> GetDefaultPermissions()
        {
            return new Dictionary<string, bool>
            {
                { "ExchangeRate.ViewLatest", true },
                { "ExchangeRate.ConvertAmount", true },
                { "ExchangeRate.ViewHistory", true }
            };
        }

        #endregion

        #region Common When Methods

        protected async Task<HttpResponseMessage> WhenSendingGetRequest(string endpoint)
        {
            return await Client.GetAsync(endpoint);
        }

        protected async Task<HttpResponseMessage> WhenSendingPostRequest<T>(string endpoint, T payload)
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            return await Client.PostAsync(endpoint, content);
        }

        protected async Task<HttpResponseMessage> WhenSendingPutRequest<T>(string endpoint, T payload)
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            return await Client.PutAsync(endpoint, content);
        }

        protected async Task<HttpResponseMessage> WhenSendingDeleteRequest(string endpoint)
        {
            return await Client.DeleteAsync(endpoint);
        }

        #endregion

        #region Common Then Methods

        protected void ThenStatusCodeShouldBe(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
        {
            response.StatusCode.Should().Be(expectedStatusCode, 
                because: $"the API should return {expectedStatusCode} for this scenario");
        }

        protected async Task<T> ThenResponseShouldContain<T>(HttpResponseMessage response)
        {
            response.Should().NotBeNull();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();

            var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            return result!;
        }

        protected async Task ThenResponseShouldContainError(HttpResponseMessage response, string expectedErrorMessage)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain(expectedErrorMessage,
                because: "error responses should provide meaningful feedback");
        }

        protected void ThenResponseShouldBeSuccessful(HttpResponseMessage response)
        {
            response.IsSuccessStatusCode.Should().BeTrue(
                because: $"expected successful response but got {response.StatusCode}");
        }

        protected void ThenResponseShouldBeUnauthorized(HttpResponseMessage response)
        {
            ThenStatusCodeShouldBe(response, HttpStatusCode.Unauthorized);
        }

        protected void ThenResponseShouldBeForbidden(HttpResponseMessage response)
        {
            ThenStatusCodeShouldBe(response, HttpStatusCode.Forbidden);
        }

        protected void ThenResponseShouldBeBadRequest(HttpResponseMessage response)
        {
            ThenStatusCodeShouldBe(response, HttpStatusCode.BadRequest);
        }

        #endregion

        #region Helper Methods

        protected string CreateEndpoint(string basePath, params object[] parameters)
        {
            if (parameters?.Length > 0)
            {
                return $"{basePath}/{string.Join("/", parameters)}";
            }
            return basePath;
        }

        #endregion
    }

    /// <summary>
    /// Enhanced test data builder with fluent API
    /// </summary>
    public class TestDataBuilder
    {
        public TestAuthHelper.TestTokenRequest CreateAuthTokenWith(
            string username = "test-user",
            Dictionary<string, bool>? permissions = null,
            int expirationInSeconds = 100)
        {
            return new TestAuthHelper.TestTokenRequest
            {
                Username = username,
                Permissions = permissions ?? new Dictionary<string, bool>(),
                ExpirationInSeconds = expirationInSeconds
            };
        }

        public object CreateConversionRequest(
            string fromCurrency, 
            string toCurrency, 
            decimal amount, 
            string provider = "frankfurter")
        {
            return new
            {
                Provider = provider,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                AmountInCents = amount
            };
        }

        public CurrencyConversionRequestBuilder ConversionRequest()
        {
            return new CurrencyConversionRequestBuilder();
        }
    }

    /// <summary>
    /// Fluent builder for currency conversion requests
    /// </summary>
    public class CurrencyConversionRequestBuilder
    {
        private string _fromCurrency = "EUR";
        private string _toCurrency = "USD";
        private decimal _amount = 100;
        private string _provider = "frankfurter";

        public CurrencyConversionRequestBuilder From(string currency)
        {
            _fromCurrency = currency;
            return this;
        }

        public CurrencyConversionRequestBuilder To(string currency)
        {
            _toCurrency = currency;
            return this;
        }

        public CurrencyConversionRequestBuilder WithAmount(decimal amount)
        {
            _amount = amount;
            return this;
        }

        public CurrencyConversionRequestBuilder UsingProvider(string provider)
        {
            _provider = provider;
            return this;
        }

        public object Build()
        {
            return new
            {
                Provider = _provider,
                FromCurrency = _fromCurrency,
                ToCurrency = _toCurrency,
                AmountInCents = _amount
            };
        }
    }
}