namespace CurrencyConverter.Models.Configurations.HttpHandler
{
    public record HttpResilienceOptions
    {
        public RetryPolicyOptions RetryPolicy { get; set; } = new();
        public CircuitBreakerOptions CircuitBreakerPolicy { get; set; } = new();
    }
}
