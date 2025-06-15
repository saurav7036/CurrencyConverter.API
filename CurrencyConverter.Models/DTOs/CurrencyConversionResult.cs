namespace CurrencyConverter.Models.DTOs
{
    public record CurrencyConversionResult
    {
        public string BaseCurrency { get; set; } = string.Empty;
        public string TargetCurrency { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal ConvertedAmount { get; set; }
        public DateTime RateTimestamp { get; set; }
    }
}
