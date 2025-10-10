using System;
using System.Net.Mail;
using System.Threading.Tasks;
using KinoWebsite_Backend.API.Controllers;
using KinoWebsite_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KinoWebsite_Backend.Tests.Controllers
{
    public class EmailsControllerTests
    {
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly EmailsController _controller;

        public EmailsControllerTests()
        {
            _emailServiceMock = new Mock<IEmailService>();
            _controller = new EmailsController(_emailServiceMock.Object);
        }

        [Fact]
        public async Task SendEmail_ReturnsOk_WhenEmailSentSuccessfully()
        {
            // Arrange
            var receptor = "test@example.com";
            var subject = "Test Subject";
            var body = "Test Body";

            _emailServiceMock
                .Setup(s => s.SendEmailAsync(receptor, subject, body, null, null))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SendEmail(receptor, subject, body);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Contains("Email sent successfully", okResult.Value.ToString());
        }

        [Fact]
        public async Task SendEmail_Returns500_WhenSmtpExceptionThrown()
        {
            // Arrange
            var receptor = "test@example.com";
            var subject = "Test Subject";
            var body = "Test Body";

            _emailServiceMock
                .Setup(s => s.SendEmailAsync(receptor, subject, body, null, null))
                .ThrowsAsync(new SmtpException("SMTP error"));

            // Act
            var result = await _controller.SendEmail(receptor, subject, body);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Contains("SMTP error", objectResult.Value.ToString());
        }

        [Fact]
        public async Task SendEmail_Returns500_WhenGeneralExceptionThrown()
        {
            // Arrange
            var receptor = "test@example.com";
            var subject = "Test Subject";
            var body = "Test Body";

            _emailServiceMock
                .Setup(s => s.SendEmailAsync(receptor, subject, body, null, null))
                .ThrowsAsync(new Exception("General error"));

            // Act
            var result = await _controller.SendEmail(receptor, subject, body);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Contains("General error", objectResult.Value.ToString());
        }
    }
}
