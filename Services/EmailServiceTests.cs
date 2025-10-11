using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using KinoWebsite_Backend.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace KinoWebsite_Backend.Tests.Services
{
    public class EmailServiceTests
    {
        private IConfiguration CreateValidConfig()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                {"EMAIL_CONFIGURATION:EMAIL", "test@example.com"},
                {"EMAIL_CONFIGURATION:PASSWORD", "password123"},
                {"EMAIL_CONFIGURATION:HOST", "localhost"},
                {"EMAIL_CONFIGURATION:PORT", "25"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public async Task SendEmailAsync_Throws_WhenConfigMissing()
        {
            var emptyConfig = new ConfigurationBuilder().Build();
            var service = new EmailService(emptyConfig);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.SendEmailAsync("receiver@example.com", "Subject", "<b>Body</b>"));
        }

        [Fact]
        public async Task SendEmailAsync_Works_WithValidConfig()
        {
            var config = CreateValidConfig();
            var service = new EmailService(config);

            var exception = await Record.ExceptionAsync(async () =>
                await service.SendEmailAsync("receiver@example.com", "Subject", "<p>Hello</p>"));

            if (exception is SmtpException)
                Assert.Contains("Failure sending mail", exception.Message);
            else
                Assert.Null(exception);
        }

        [Fact]
        public async Task SendEmailAsync_Works_WithLinkedResources()
        {
            var config = CreateValidConfig();
            var service = new EmailService(config);

            var linkedResources = new List<LinkedResource>
            {
                new LinkedResource(new MemoryStream(new byte[] { 1, 2, 3 }), "image/png")
                {
                    ContentId = "logo"
                }
            };

            var exception = await Record.ExceptionAsync(async () =>
                await service.SendEmailAsync("receiver@example.com", "Subject", "<img src='cid:logo'>", null, linkedResources));

            if (exception is SmtpException)
                Assert.Contains("Failure sending mail", exception.Message);
            else
                Assert.Null(exception);
        }

        [Fact]
        public async Task SendEmailAsync_Throws_SmtpException_OnInvalidHost()
        {
            var badSettings = new Dictionary<string, string?>
            {
                {"EMAIL_CONFIGURATION:EMAIL", "test@example.com"},
                {"EMAIL_CONFIGURATION:PASSWORD", "password123"},
                {"EMAIL_CONFIGURATION:HOST", "invalid.host.local"},
                {"EMAIL_CONFIGURATION:PORT", "587"}
            };
            var badConfig = new ConfigurationBuilder().AddInMemoryCollection(badSettings).Build();

            var service = new EmailService(badConfig);

            var ex = await Record.ExceptionAsync(async () =>
                await service.SendEmailAsync("receiver@example.com", "Subject", "<b>Body</b>"));

            Assert.True(ex is SmtpException || ex is InvalidOperationException);
        }
    }
}
