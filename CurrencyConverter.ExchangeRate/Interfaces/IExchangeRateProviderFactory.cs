namespace CurrencyConverter.ExchangeRate.Interfaces
{
    public interface IExchangeRateProviderFactory
    {
        IExchangeRateProvider GetProvider(string key);
    }
}
