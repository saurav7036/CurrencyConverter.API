namespace CurrencyConverter.ExchangeRate.Infrastructure.Http
{
    public interface IHttpClientWrapper
    {
        Task<HttpResponseMessage> GetAsync(string clientName, string endpoint);
    }
}
