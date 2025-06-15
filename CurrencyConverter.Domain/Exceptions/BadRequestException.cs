using Microsoft.AspNetCore.Http;

namespace CurrencyConverter.Domain.Exceptions
{
    public class BadRequestException : ServiceException
    {
        public BadRequestException(string message)
            : base("bad_request", message, StatusCodes.Status400BadRequest)
        {
        }
    }
}
