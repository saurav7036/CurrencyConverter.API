using System.ComponentModel.DataAnnotations;

namespace CurrencyConverter.API.ViewModels.Request
{
    public class HistoricalRatesQueryRequest : CurrencyQueryRequest
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 10;
    }
}
