namespace CurrencyConverter.Models.DTOs.Caching
{
    public class HistoricalRateCacheEntry
    {
        public SortedDictionary<DateTime, HistoricalRateDto> Rates { get; set; } = new();
        public DateTime LastApiCallUtc { get; set; }
    }
}
    