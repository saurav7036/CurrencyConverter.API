using CurrencyConverter.ExchangeRate;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using FluentAssertions;

namespace CurrencyConverter.Tests.Unit.HealthChecks
{
    public class FrankfurterHealthCheckTests
    {
        private readonly Mock<ILogger<FrankfurterHealthCheck>> _loggerMock = new();

        private IHttpClientFactory CreateHttpClientFactory(HttpResponseMessage response)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(response);

            var client = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("https://api.frankfurter.app/")
            };

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient("FrankfurterClient")).Returns(client);

            return factoryMock.Object;
        }

        [Fact]
        public async Task CheckHealthAsync_ReturnsHealthy_WhenStatusCodeIs200()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var factory = CreateHttpClientFactory(response);
            var check = new FrankfurterHealthCheck(factory, _loggerMock.Object);

            // Act
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            // Assert
            result.Status.Should().Be(HealthStatus.Healthy);
            result.Description.Should().Contain("reachable");
        }

        [Fact]
        public async Task CheckHealthAsync_ReturnsUnhealthy_WhenStatusCodeIs500()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var factory = CreateHttpClientFactory(response);
            var check = new FrankfurterHealthCheck(factory, _loggerMock.Object);

            // Act
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy);
        }

        [Fact]
        public async Task CheckHealthAsync_ReturnsUnhealthy_WhenExceptionIsThrown()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("API down"));

            var client = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("https://api.frankfurter.app/")
            };

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient("FrankfurterClient")).Returns(client);

            var check = new FrankfurterHealthCheck(factoryMock.Object, _loggerMock.Object);

            // Act
            var result = await check.CheckHealthAsync(new HealthCheckContext());

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Exception.Should().BeOfType<HttpRequestException>();
            result.Description.Should().Contain("unreachable");
        }
    }
}
