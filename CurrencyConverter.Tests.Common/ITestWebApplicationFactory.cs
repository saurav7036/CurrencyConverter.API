using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Tests.Common
{
    public interface ITestWebApplicationFactory
    {
        void ConfigureTestServices(Action<IServiceCollection> config);
    }
}
