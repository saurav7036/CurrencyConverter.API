using System.ComponentModel.DataAnnotations;

namespace CurrencyConverter.API.ViewModels.Request
{
    public class CurrencyQueryRequest
    {
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string BaseCurrency { get; set; } = default!;

        [Required]
        public string Provider { get; set; } = default!;

        public void Normalize()
        {
            BaseCurrency = BaseCurrency!.ToUpperInvariant();
            Provider = Provider!.ToLowerInvariant();
        }
    }
}
