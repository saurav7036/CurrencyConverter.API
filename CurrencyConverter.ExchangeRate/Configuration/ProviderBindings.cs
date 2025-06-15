using CurrencyConverter.ExchangeRate.Infrastructure.Http;
using CurrencyConverter.ExchangeRate.Interfaces;
using CurrencyConverter.Models.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.ExchangeRate.Configuration
{
    public static class ProviderBindings
    {
        public static IServiceCollection Register(this IServiceCollection services, IConfiguration configuration)
        {
            AddFrankfurterHttpClient(services, configuration);

            services.AddScoped<IHttpClientWrapper, HttpClientWrapper>();
            services.AddScoped<IExchangeRateProviderFactory, ExchangeRateProviderFactory>();
            services.AddKeyedScoped<IExchangeRateProvider, FrankfurterProvider>("frankfurter");

            return services;

            static void AddFrankfurterHttpClient(IServiceCollection services, IConfiguration configuration)
            {
                var providerOptions = configuration
                            .GetSection("ExchangeRate:Providers")
                            .Get<List<ExchangeRateProviderOptions>>()
                            ?.FirstOrDefault(p => p.Name.Equals("frankfurter", StringComparison.OrdinalIgnoreCase));

                if (providerOptions == null)
                {
                    throw new InvalidOperationException("Frankfurter provider configuration not found.");
                }

                services.AddHttpClient("FrankfurterClient", client =>
                {
                    client.BaseAddress = new Uri("https://api.frankfurter.app/");
                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                });
            }
        }
    }
}
