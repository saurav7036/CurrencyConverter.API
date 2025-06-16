using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.ExchangeRate
{
    public class FrankfurterHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FrankfurterHealthCheck> _logger;

        public FrankfurterHealthCheck(IHttpClientFactory httpClientFactory, ILogger<FrankfurterHealthCheck> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("FrankfurterClient");
                var response = await client.GetAsync("latest", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Frankfurter health check succeeded with status code: {StatusCode}", response.StatusCode);
                    return HealthCheckResult.Healthy("Frankfurter API is reachable.");
                }

                _logger.LogWarning("Frankfurter health check failed with status code: {StatusCode}", response.StatusCode);
                return HealthCheckResult.Unhealthy($"Frankfurter returned {response.StatusCode}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Frankfurter health check threw an exception.");
                return HealthCheckResult.Unhealthy("Frankfurter API is unreachable.", ex);
            }
        }
    }
}
