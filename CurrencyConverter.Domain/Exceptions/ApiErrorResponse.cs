namespace CurrencyConverter.Domain.Exceptions
{
    public record ApiErrorResponse
    {
        public int StatusCode { get; set; }
        public string ErrorCode { get; set; } = "internal_error";
        public string Message { get; set; } = "An unexpected error occurred.";

        public string[] Details { get; set; } = Array.Empty<string>();
    }
}
