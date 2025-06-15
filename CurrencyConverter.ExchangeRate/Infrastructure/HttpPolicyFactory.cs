using CurrencyConverter.Models.Configurations.HttpHandler;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace CurrencyConverter.ExchangeRate.Infrastructure
{
    public static class HttpPolicyFactory
    {
        public static IAsyncPolicy<HttpResponseMessage> CreateResiliencePolicy(
            RetryPolicyOptions retryOptions,
            CircuitBreakerOptions circuitOptions,
            ILogger logger)
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: retryOptions.RetryCount,
                    sleepDurationProvider: attempt =>
                        TimeSpan.FromSeconds(Math.Pow(retryOptions.InitialDelaySeconds, attempt)),
                    onRetry: (outcome, delay, attempt, _) =>
                    {
                        logger.LogWarning("[RETRY] Attempt {Attempt} after {Delay}s due to: {Error}",
                            attempt, delay.TotalSeconds, outcome.Exception?.Message);
                    });

            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: circuitOptions.HandledEventsAllowedBeforeBreaking,
                    durationOfBreak: TimeSpan.FromSeconds(circuitOptions.DurationOfBreakSeconds),
                    onBreak: (outcome, breakDelay) =>
                    {
                        logger.LogError("[CIRCUIT BREAK] Opened for {Delay}s due to: {Error}",
                            breakDelay.TotalSeconds, outcome.Exception?.Message);
                    },
                    onReset: () =>
                    {
                        logger.LogInformation("[CIRCUIT BREAK] Closed. Service recovered.");
                    },
                    onHalfOpen: () =>
                    {
                        logger.LogWarning("[CIRCUIT BREAK] Half-open. Testing downstream service.");
                    });

            var fallbackPolicy = Policy<HttpResponseMessage>
                    .Handle<Exception>()
                    .FallbackAsync(
                        fallbackAction: (_, _) =>
                        {
                            throw new HttpRequestException("Service temporarily unavailable. Fallback triggered.");
                        },
                        onFallbackAsync: (outcome, _) =>
                        {
                            logger.LogError(outcome.Exception, "[FALLBACK] Triggered due to: {Error}", outcome.Exception?.Message);
                            return Task.CompletedTask;
                        });

            return Policy.WrapAsync(fallbackPolicy, retryPolicy, circuitBreakerPolicy);
        }
    }
}
