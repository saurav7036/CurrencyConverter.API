namespace CurrencyConverter.Models.Configurations
{
    public record ExchangeRateSettings
    {
        public List<ExchangeRateProviderOptions> Providers { get; set; } = [];
    }
}
