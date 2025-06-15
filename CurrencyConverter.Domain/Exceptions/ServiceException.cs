namespace CurrencyConverter.Domain.Exceptions
{
    public class ServiceException : Exception
    {
        public string ErrorCode { get; }
        public int StatusCode { get; }

        public ServiceException(string errorCode, string message, int statusCode = 400)
            : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }
}
