using CurrencyConverter.ExchangeRate.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.ExchangeRate
{
    internal class ExchangeRateProviderFactory : IExchangeRateProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ExchangeRateProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IExchangeRateProvider GetProvider(string key)
        {
            var provider = _serviceProvider.GetKeyedService<IExchangeRateProvider>(key);
            if (provider is null)
            {
                throw new NotSupportedException($"Currency provider '{key}' is not registered.");
            }

            return provider;
        }
    }
}
