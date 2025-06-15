namespace CurrencyConverter.Models.Configurations
{
    public class ExchangeRateProviderOptions
    {
        public string Name { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public bool IsFloatingRate { get; set; }
        public int LatestRateTtlSeconds { get; set; }
        public TimeSpan UpdateInterval => TimeSpan.FromSeconds(LatestRateTtlSeconds);
    }
}
