namespace CurrencyConverter.Models.DTOs.Caching
{
    public record LatestRateCacheEntry
    {
        public LatestRateDto Rate { get; set; } = default!;
        public DateTime LastApiCallUtc { get; set; }
    }
}
