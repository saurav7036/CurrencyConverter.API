using CurrencyConverter.ExchangeRate.Infrastructure.Http;
using CurrencyConverter.Models.DTOs;
using CurrencyConverter.Tests.Common;
using FluentAssertions;
using Moq;
using System.Net;
using System.Text;

namespace CurrencyConverter.Tests.Behavioral
{
    /// <summary>
    /// Behavioral specifications for exchange rates functionality
    /// Covers getting latest and historical exchange rates
    /// </summary>
    public class ExchangeRatesSpecifications : BaseBehavioralSpecification
    {
        private readonly Mock<IHttpClientWrapper> _httpClientWrapperMock;

        public ExchangeRatesSpecifications(TestWebApplicationFactory<Program> factory) : base(factory)
        {
            _httpClientWrapperMock = SetupHttpClientMock();
        }

        #region Latest Exchange Rates Scenarios

        [Fact(DisplayName = "Given authenticated user, when requesting latest rates, then should return current exchange rates")]
        public async Task GetLatestRates_ShouldReturnRates_WhenAuthenticated()
        {
            // Given
            await GivenAuthenticatedUser();
            GivenExchangeRateProvider("EUR", new Dictionary<string, decimal>
            {
                { "USD", 1.25m },
                { "GBP", 0.85m },
                { "JPY", 130.45m }
            });

            // When
            var response = await WhenRequestingLatestRates("EUR", "frankfurter");

            // Then
            ThenResponseShouldBeSuccessful(response);
            var rates = await ThenResponseShouldContain<ExchangeRatesResult>(response);
            rates.BaseCurrency.Should().Be("EUR");
            rates.Rates.Should().ContainKey("USD").WhoseValue.Should().Be(1.25m);
            rates.Rates.Should().ContainKey("GBP").WhoseValue.Should().Be(0.85m);
        }

        [Fact(DisplayName = "Given unauthenticated user, when requesting latest rates, then should return unauthorized")]
        public async Task GetLatestRates_ShouldReturnUnauthorized_WhenNotAuthenticated()
        {
            // Given - No authentication
            
            // When
            var response = await WhenRequestingLatestRates("EUR", "frankfurter");

            // Then
            ThenResponseShouldBeUnauthorized(response);
        }

        [Fact(DisplayName = "Given user without view permission, when requesting latest rates, then should return forbidden")]
        public async Task GetLatestRates_ShouldReturnForbidden_WhenUserLacksPermission()
        {
            // Given
            await GivenUserWithoutViewPermission();

            // When
            var response = await WhenRequestingLatestRates("EUR", "frankfurter");

            // Then
            ThenResponseShouldBeForbidden(response);
        }

        [Theory(DisplayName = "Given various base currencies, when requesting latest rates, then should return appropriate rates")]
        [InlineData("USD")]
        [InlineData("GBP")]
        [InlineData("JPY")]
        public async Task GetLatestRates_ShouldSupportMultipleBaseCurrencies(string baseCurrency)
        {
            // Given
            await GivenAuthenticatedUser();
            GivenExchangeRateProvider(baseCurrency, new Dictionary<string, decimal>
            {
                { "EUR", 1.0m }
            });

            // When
            var response = await WhenRequestingLatestRates(baseCurrency, "frankfurter");

            // Then
            ThenResponseShouldBeSuccessful(response);
            var rates = await ThenResponseShouldContain<ExchangeRatesResult>(response);
            rates.BaseCurrency.Should().Be(baseCurrency);
        }

        #endregion

        #region Historical Exchange Rates Scenarios

        [Fact(DisplayName = "Given valid date range, when requesting historical rates, then should return time series data")]
        public async Task GetHistoricalRates_ShouldReturnTimeSeries_WhenValidDateRange()
        {
            // Given
            await GivenAuthenticatedUser();
            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;
            GivenHistoricalExchangeRateProvider("EUR", "USD", startDate, endDate);

            // When
            var response = await WhenRequestingHistoricalRates("EUR", "USD", startDate, endDate);

            // Then
            ThenResponseShouldBeSuccessful(response);
            var historicalRates = await ThenResponseShouldContain<HistoricalExchangeRatesResult>(response);
            historicalRates.BaseCurrency.Should().Be("EUR");
            historicalRates.TargetCurrency.Should().Be("USD");
            historicalRates.TimeSeriesData.Should().NotBeEmpty();
        }

        [Fact(DisplayName = "Given invalid date range, when requesting historical rates, then should return bad request")]
        public async Task GetHistoricalRates_ShouldReturnBadRequest_WhenInvalidDateRange()
        {
            // Given
            await GivenAuthenticatedUser();
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(-30); // End before start

            // When
            var response = await WhenRequestingHistoricalRates("EUR", "USD", startDate, endDate);

            // Then
            ThenResponseShouldBeBadRequest(response);
            await ThenResponseShouldContainError(response, "start date must be before end date");
        }

        #endregion

        #region External Service Integration Scenarios

        [Fact(DisplayName = "Given external service is unavailable, when requesting rates, then should return service unavailable")]
        public async Task GetLatestRates_ShouldReturnServiceUnavailable_WhenExternalServiceDown()
        {
            // Given
            await GivenAuthenticatedUser();
            GivenExternalServiceIsUnavailable();

            // When
            var response = await WhenRequestingLatestRates("EUR", "frankfurter");

            // Then
            ThenStatusCodeShouldBe(response, HttpStatusCode.ServiceUnavailable);
        }

        [Fact(DisplayName = "Given external service returns invalid data, when requesting rates, then should return bad gateway")]
        public async Task GetLatestRates_ShouldReturnBadGateway_WhenExternalServiceReturnsInvalidData()
        {
            // Given
            await GivenAuthenticatedUser();
            GivenExternalServiceReturnsInvalidData();

            // When
            var response = await WhenRequestingLatestRates("EUR", "frankfurter");

            // Then
            ThenStatusCodeShouldBe(response, HttpStatusCode.BadGateway);
        }

        #endregion

        #region Given Methods (Setup)

        private async Task GivenUserWithoutViewPermission()
        {
            var permissions = new Dictionary<string, bool>
            {
                { "ExchangeRate.ViewLatest", false }, // Denied
                { "ExchangeRate.ConvertAmount", true },
                { "ExchangeRate.ViewHistory", false } // Denied
            };
            await GivenAuthenticatedUser(permissions);
        }

        private Mock<IHttpClientWrapper> SetupHttpClientMock()
        {
            var mock = Factory.Mock<IHttpClientWrapper>();
            return mock;
        }

        private void GivenExchangeRateProvider(string baseCurrency, Dictionary<string, decimal> rates)
        {
            var ratesJson = string.Join(",", rates.Select(r => $"\"{r.Key}\":{r.Value}"));
            var responseContent = $"{{\"base\":\"{baseCurrency}\",\"rates\":{{{ratesJson}}}}}";
            
            _httpClientWrapperMock
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                });
        }

        private void GivenHistoricalExchangeRateProvider(string baseCurrency, string targetCurrency, DateTime startDate, DateTime endDate)
        {
            var timeSeriesData = new Dictionary<string, decimal>();
            var currentDate = startDate;
            var random = new Random(42); // Fixed seed for reproducible tests

            while (currentDate <= endDate)
            {
                timeSeriesData[currentDate.ToString("yyyy-MM-dd")] = 1.20m + (decimal)(random.NextDouble() * 0.1 - 0.05);
                currentDate = currentDate.AddDays(1);
            }

            var timeSeriesJson = string.Join(",", timeSeriesData.Select(t => $"\"{t.Key}\":{t.Value}"));
            var responseContent = $"{{\"base\":\"{baseCurrency}\",\"start_date\":\"{startDate:yyyy-MM-dd}\",\"end_date\":\"{endDate:yyyy-MM-dd}\",\"rates\":{{{timeSeriesJson}}}}}";

            _httpClientWrapperMock
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                });
        }

        private void GivenExternalServiceIsUnavailable()
        {
            _httpClientWrapperMock
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }

        private void GivenExternalServiceReturnsInvalidData()
        {
            _httpClientWrapperMock
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("invalid json", Encoding.UTF8, "application/json")
                });
        }

        #endregion

        #region When Methods (Actions)

        private async Task<HttpResponseMessage> WhenRequestingLatestRates(string baseCurrency, string provider)
        {
            var endpoint = $"api/v1/exchange-rates/latest?BaseCurrency={baseCurrency}&Provider={provider}";
            return await WhenSendingGetRequest(endpoint);
        }

        private async Task<HttpResponseMessage> WhenRequestingHistoricalRates(
            string baseCurrency, string targetCurrency, DateTime startDate, DateTime endDate)
        {
            var endpoint = $"api/v1/exchange-rates/historical?BaseCurrency={baseCurrency}&TargetCurrency={targetCurrency}&StartDate={startDate:yyyy-MM-dd}&EndDate={endDate:yyyy-MM-dd}";
            return await WhenSendingGetRequest(endpoint);
        }

        #endregion
    }

    #region DTOs for Test Responses

    public class ExchangeRatesResult
    {
        public string BaseCurrency { get; set; } = string.Empty;
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }

    public class HistoricalExchangeRatesResult
    {
        public string BaseCurrency { get; set; } = string.Empty;
        public string TargetCurrency { get; set; } = string.Empty;
        public Dictionary<string, decimal> TimeSeriesData { get; set; } = new();
    }

    #endregion
}