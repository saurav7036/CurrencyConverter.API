namespace CurrencyConverter.Models.DTOs
{
    public record HistoricalRateDto
    {
        public DateTime Date { get; set; }
        public string BaseCurrency { get; set; } = default!;
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
