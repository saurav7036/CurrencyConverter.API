using CurrencyConverter.Cache.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Cache.Configuration
{
    public static class CacheBindings
    {
        public static IServiceCollection Register(this IServiceCollection services)
        {
            services.AddScoped<ICurrencyHistoricalRateCache, InMemoryCurrencyHistoricalRateCache>();
            services.AddScoped<ICurrencyLatestRateCache, InMemoryCurrencyLatestRateCache>();
            return services;
        }
    }
}
