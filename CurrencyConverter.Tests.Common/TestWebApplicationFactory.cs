using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Tests.Common
{
    public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup>, ITestWebApplicationFactory
    where TStartup : class
    {
        private readonly List<Action<IServiceCollection>> _configureActions = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                foreach (var action in _configureActions)
                    action(services);
            });
        }

        public void ConfigureTestServices(Action<IServiceCollection> config)
        {
            _configureActions.Add(config);
        }
    }
}
