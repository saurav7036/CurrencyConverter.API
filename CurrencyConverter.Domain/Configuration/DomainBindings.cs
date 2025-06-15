using CurrencyConverter.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Domain.Configuration
{
    public static class DomainBindings
    {
        public static IServiceCollection Register(this IServiceCollection services)
        {
            services.AddScoped<ICurrencyExchangeRateService, CurrencyExchangeRateService>();
            return services;
        }
    }
}
