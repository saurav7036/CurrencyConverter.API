namespace CurrencyConverter.Models.DTOs.Frankfurter
{
    public record FrankfurterLatestApiResponse
    {
        public string Base { get; set; } = default!;
        public DateTime Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
