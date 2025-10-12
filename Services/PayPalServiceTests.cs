using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using KinoWebsite_Backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace KinoWebsite_Backend.Tests.Services
{
    public class PayPalServiceTests
    {
        private PayPalService CreateService(out Mock<HttpMessageHandler> handlerMock)
        {
            var configData = new Dictionary<string, string?>
            {
                {"PayPalSettings:URL", "https://api.sandbox.paypal.com"},
                {"PayPalSettings:ClientId", "dummyId"},
                {"PayPalSettings:Secret", "dummySecret"}
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var client = new HttpClient(handlerMock.Object);
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            var logger = Mock.Of<ILogger<PayPalService>>();

            return new PayPalService(configuration, httpClientFactory.Object, logger);
        }

       
        [Fact]
        public async Task CreateOrderAsync_ReturnsNull_WhenTokenFails()
        {
            // Arrange
            var service = CreateService(out var handlerMock);

            var failResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\": \"invalid_client\"}")
            };

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(failResponse);

            // Act
            var result = await service.CreateOrderAsync("EUR", "10.00");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateOrderAsync_ReturnsNull_WhenOrderFails()
        {
            // Arrange
            var service = CreateService(out var handlerMock);

            var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\": \"ABC123\"}", Encoding.UTF8, "application/json")
            };

            var failResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\": \"invalid_request\"}")
            };

            handlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(tokenResponse)
                .ReturnsAsync(failResponse);

            // Act
            var result = await service.CreateOrderAsync("EUR", "10.00");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateOrderAsync_Handles_Exception_Gracefully()
        {
            // Arrange
            var service = CreateService(out var handlerMock);

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Network failure"));

            // Act
            var result = await service.CreateOrderAsync("EUR", "10.00");

            // Assert
            Assert.Null(result);
        }
    }
}
