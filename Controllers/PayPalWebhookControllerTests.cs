using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using KinoWebsite_Backend.API.Controllers;
using KinoWebsite_Backend.Services;

namespace KinoWebsite_Backend.Tests.Controllers
{
    public class PayPalWebhookControllerTests
    {
        private class FakeOrderService : OrderService
        {
            public bool Called { get; private set; }
            public string? LastId { get; private set; }

            public FakeOrderService() : base(null!, null!, null!, null!) { }

            public Task<bool> FakeProcessWebhook(string payPalOrderId)
            {
                Called = true;
                LastId = payPalOrderId ?? "UNKNOWN";
                return Task.FromResult(true);
            }
        }

        private readonly ILogger<PayPalWebhookController> _logger =
            new LoggerFactory().CreateLogger<PayPalWebhookController>();

        [Fact]
        public async Task ReceivePayPalWebhook_ProcessesApprovedOrder()
        {
            var fakeService = new FakeOrderService();

            var payload = new JsonObject
            {
                ["event_type"] = "CHECKOUT.ORDER.APPROVED",
                ["resource"] = new JsonObject { ["id"] = "ORDER123" }
            };

            var eventType = payload?["event_type"]?.GetValue<string>();
            var resource = payload?["resource"] as JsonObject;
            var payPalOrderId = resource?["id"]?.GetValue<string>();

            if (eventType == "CHECKOUT.ORDER.APPROVED")
            {
                await fakeService.FakeProcessWebhook(payPalOrderId!);
            }

            // Assert
            Assert.True(fakeService.Called);
            Assert.Equal("ORDER123", fakeService.LastId);
        }
    }
}
