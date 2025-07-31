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
        private readonly string _startDate;
        private readonly string _endDate;
        private readonly string _apiUrl;

        public GetHistoricalExchangeRatesSpecification(TestWebApplicationFactory<Program> factory)
        {
            _httpClientWrapperMock = factory.Mock<IHttpClientWrapper>();

            _startDate = DateTime.UtcNow.Date.AddDays(-2).ToString("yyyy-MM-dd");
            _endDate = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
            _apiUrl = $"api/v1/exchange-rates/history?StartDate={_startDate}&EndDate={_endDate}&PageNumber=1&PageSize=2&BaseCurrency=eur&Provider=frankfurter";

            SetUpMockResponse();
            _client = factory.CreateClient();
        }

        [Fact(DisplayName = "Get historical exchange rates for EUR from Frankfurter with dynamic dates")]
        public async Task GetHistoricalExchangeRate_ReturnsRates_ForEUR_FromFrankfurter()
        {
            await AddJwtTokenHeader();

            var response = await _client.GetAsync(_apiUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PagedResult<HistoricalRateDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(result);
            Assert.Equal(1, result!.TotalCount);

            var item = result.Items.FirstOrDefault();
            Assert.NotNull(item);
            Assert.True(item!.Rates.ContainsKey("USD"));
            Assert.Equal(1.10m, item.Rates["USD"]);
        }


        [Fact(DisplayName = "Returns 403 Forbidden when user lacks required ViewHistory permission")]
        public async Task GetHistoricalExchangeRate_Forbidden_WhenPermissionNotGranted()
        {
            await TestAuthHelper.AddJwtTokenAsync(_client, new TestAuthHelper.TestTokenRequest
            {
                Username = "test-user",
                Permissions = new Dictionary<string, bool>
                {
                    { "ExchangeRate.ViewLatest", true },
                    { "ExchangeRate.ViewHistory", false }
                },
                ExpirationInSeconds = 100
            });

            var response = await _client.GetAsync(_apiUrl);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory(DisplayName = "Returns 400 BadRequest for invalid date ranges")]
        [InlineData("2020-01-01", "2020-01-10", "Only historical data within")]
        [InlineData("2025-01-10", "2025-01-01", "'from' date must be before")]
        [InlineData("2025-01-01", "2099-01-01", "'to' date cannot be in the future")]
        public async Task GetHistoricalExchangeRate_ReturnsBadRequest_ForInvalidDateRanges(string from, string to, string expectedError)
        {
            await AddJwtTokenHeader();

            var url = $"api/v1/exchange-rates/history?StartDate={from}&EndDate={to}&PageNumber=1&PageSize=2&BaseCurrency=eur&Provider=frankfurter";
            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains(expectedError, body);
        }

        [Fact(DisplayName = "Returns 400 Bad Request when provider is invalid")]
        public async Task GetHistoricalExchangeRate_ReturnsError_WhenProviderMetadataMissing()
        {
            await AddJwtTokenHeader();

            var url = $"api/v1/exchange-rates/history?StartDate={_startDate}&EndDate={_endDate}&PageNumber=1&PageSize=2&BaseCurrency=eur&Provider=unknownprovider";
            var response = await _client.GetAsync(url);

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

        private void SetUpMockResponse()
        {
            var response = new TestFrankfurterHistoricalApiResponse
            {
                Base = "EUR",
                StartDate = DateTime.Parse(_startDate),
                EndDate = DateTime.Parse(_endDate),
                RatesByDate = new Dictionary<string, Dictionary<string, decimal>>
                {
                    { _startDate, new Dictionary<string, decimal> { { "USD", 1.10m } } }
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
        }
    }
}
