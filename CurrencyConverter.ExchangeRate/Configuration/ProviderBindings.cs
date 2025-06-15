using CurrencyConverter.ExchangeRate.Infrastructure;
using CurrencyConverter.ExchangeRate.Infrastructure.Http;
using CurrencyConverter.ExchangeRate.Interfaces;
using CurrencyConverter.Models.Configurations;
using CurrencyConverter.Models.Configurations.HttpHandler;
using CurrencyConverter.Models.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using System.Net.Http.Headers;

namespace CurrencyConverter.ExchangeRate.Configuration
{
    public static class ProviderBindings
    {
        private const string FrankfurterPolicyName = "FrankfurterPolicy";

        public static IServiceCollection Register(this IServiceCollection services, IConfiguration configuration)
        {
            // Step 1: Create and configure Polly policy
            var policyRegistry = new PolicyRegistry();

            var loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("FrankfurterPolicies");

            var resilienceOptions = configuration
                .GetSection(ConfigurationSectionNames.HttpResilience)
                .Get<HttpResilienceOptions>()
                ?? throw new InvalidOperationException($"{ConfigurationSectionNames.HttpResilience} config missing.");

            var policy = HttpPolicyFactory.CreateResiliencePolicy(
                resilienceOptions.RetryPolicy,
                resilienceOptions.CircuitBreakerPolicy,
                logger);

            policyRegistry.Add<IAsyncPolicy<HttpResponseMessage>>(FrankfurterPolicyName, policy);

            // Step 2: Register registry with DI
            services.AddSingleton<IReadOnlyPolicyRegistry<string>>(policyRegistry);

            // Step 3: Register named HTTP client using policy from registry
            services.AddHttpClient(HttpClientNames.FrankfurterClient, (provider, client) =>
            {
                var config = provider.GetRequiredService<IConfiguration>();

                var providers = config
                    .GetSection(ConfigurationSectionNames.ExchangeRateProviders)
                    .Get<List<ExchangeRateProviderOptions>>();

                var frankfurterOptions = providers?
                    .FirstOrDefault(p => p.Name.Equals(ProviderNames.Frankfurter, StringComparison.OrdinalIgnoreCase));

                if (frankfurterOptions == null || string.IsNullOrWhiteSpace(frankfurterOptions.BaseUrl))
                    throw new InvalidOperationException($"Configuration for provider '{ProviderNames.Frankfurter}' is missing or invalid.");

                client.BaseAddress = new Uri(frankfurterOptions.BaseUrl);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddPolicyHandlerFromRegistry(FrankfurterPolicyName); // ✅ this now works

            // Step 4: Register core services
            services.AddScoped<IHttpClientWrapper, HttpClientWrapper>();
            services.AddScoped<IExchangeRateProviderFactory, ExchangeRateProviderFactory>();
            services.AddKeyedScoped<IExchangeRateProvider, FrankfurterProvider>(ProviderNames.Frankfurter);

            return services;
        }
    }
}
