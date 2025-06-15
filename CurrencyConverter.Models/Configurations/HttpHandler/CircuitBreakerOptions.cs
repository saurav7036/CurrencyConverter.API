namespace CurrencyConverter.Models.Configurations.HttpHandler
{
    public record CircuitBreakerOptions
    {
        public int HandledEventsAllowedBeforeBreaking { get; set; }
        public int DurationOfBreakSeconds { get; set; }
    }
}
