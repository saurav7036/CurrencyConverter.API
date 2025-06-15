namespace CurrencyConverter.Models.DTOs
{
    public record TimedRateCacheEntry
    {
        public LatestRateDto Rate { get; set; } = default!;
        public DateTime Timestamp { get; set; }
    }
}
