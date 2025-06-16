using System.ComponentModel.DataAnnotations;

namespace CurrencyConverter.API.ViewModels.Request
{
    public record CurrencyConversionRequest
    {
        [Required]
        public string Provider { get; set; } = default!;

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string FromCurrency { get; set; } = default!;

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string ToCurrency { get; set; } = default!;

        [Required]
        public long AmountInCents { get; set; }
    }
}
