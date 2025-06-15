namespace CurrencyConverter.ExchangeRate.Infrastructure.Http
{
    internal class HttpClientWrapper : IHttpClientWrapper
    {
        private readonly IHttpClientFactory _factory;

        public HttpClientWrapper(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        public Task<HttpResponseMessage> GetAsync(string clientName, string endpoint)
        {
            var client = _factory.CreateClient(clientName);
            return client.GetAsync(endpoint);
        }
    }
}
