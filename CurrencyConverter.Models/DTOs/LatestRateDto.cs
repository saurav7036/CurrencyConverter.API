namespace CurrencyConverter.Models.DTOs
{
    public record LatestRateDto
    {
        public DateTime Timestamp { get; set; }
        public string BaseCurrency { get; set; } = default!;
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
