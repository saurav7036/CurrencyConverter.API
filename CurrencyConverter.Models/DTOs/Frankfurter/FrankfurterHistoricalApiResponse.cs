using System.Text.Json.Serialization;

namespace CurrencyConverter.Models.DTOs.Frankfurter
{
    public record FrankfurterHistoricalApiResponse
    {
        public string Base { get; set; }=default!;

        [JsonPropertyName("start_date")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<string, Dictionary<string, decimal>> RatesByDate { get; set; } = new();
    }
}
