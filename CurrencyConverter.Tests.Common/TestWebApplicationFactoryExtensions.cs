using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace CurrencyConverter.Tests.Common
{
    public static class TestWebApplicationFactoryExtensions
    {
        public static Mock<T> Mock<T>(this ITestWebApplicationFactory factory) where T : class
        {
            var mock = new Mock<T>();

            factory.ConfigureTestServices(services =>
            {
                services.AddSingleton(mock.Object);
            });

            return mock;
        }

        public static Mock<ILogger<T>> MockLogger<T>(this ITestWebApplicationFactory factory)
        {
            var loggerMock = new Mock<ILogger<T>>();

            factory.ConfigureTestServices(services =>
            {
                services.AddSingleton(loggerMock.Object);
            });

            return loggerMock;
        }
    }
}
