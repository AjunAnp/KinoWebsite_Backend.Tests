using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KinoWebsite_Backend.Tests.Services
{
    public class ShowStatusServiceTests
    {
        private readonly DbContextOptions<AppDbContext> _dbOptions;

        public ShowStatusServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        private IServiceProvider BuildServiceProvider(AppDbContext context, ShowService showService)
        {
            var services = new ServiceCollection();
            services.AddSingleton(context);
            services.AddSingleton(showService);
            services.AddLogging();
            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task DoWork_Should_Start_Shows_When_StartUtc_Has_Passed()
        {
            // Arrange
            using var context = new AppDbContext(_dbOptions);
            var show = new Show
            {
                StartUtc = DateTime.UtcNow.AddMinutes(-10),
                EndUtc = DateTime.UtcNow.AddHours(1),
                HasStarted = false,
                HasEnded = false,
                MovieId = 1,
                RoomId = 1,
                Language = "DE",
                Subtitle = "EN"
            };
            context.Shows.Add(show);
            await context.SaveChangesAsync();

            var mockLogger = new Mock<ILogger<ShowStatusService>>();
            var mockRoomService = new Mock<RoomService>(null);
            var showService = new ShowService(context, mockRoomService.Object);
            var provider = BuildServiceProvider(context, showService);

            var service = new ShowStatusService(mockLogger.Object, provider);

            // Act
            var doWorkMethod = typeof(ShowStatusService)
                .GetMethod("DoWork", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            doWorkMethod.Invoke(service, new object?[] { null });
            await Task.Delay(200); // kurze Verzögerung, da async void

            // Assert
            var updatedShow = await context.Shows.FirstAsync();
            Assert.True(updatedShow.HasStarted);
            Assert.False(updatedShow.HasEnded);
        }

        [Fact]
        public async Task DoWork_Should_End_Shows_When_EndUtc_Has_Passed()
        {
            // Arrange
            using var context = new AppDbContext(_dbOptions);
            var show = new Show
            {
                StartUtc = DateTime.UtcNow.AddHours(-2),
                EndUtc = DateTime.UtcNow.AddMinutes(-5),
                HasStarted = true,
                HasEnded = false,
                MovieId = 1,
                RoomId = 1,
                Language = "DE",
                Subtitle = "EN"
            };
            context.Shows.Add(show);
            await context.SaveChangesAsync();

            var mockLogger = new Mock<ILogger<ShowStatusService>>();
            var mockRoomService = new Mock<RoomService>(null);
            var showService = new ShowService(context, mockRoomService.Object);
            var provider = BuildServiceProvider(context, showService);

            var service = new ShowStatusService(mockLogger.Object, provider);

            // Act
            var doWorkMethod = typeof(ShowStatusService)
                .GetMethod("DoWork", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            doWorkMethod.Invoke(service, new object?[] { null });
            await Task.Delay(200);

            // Assert
            var updatedShow = await context.Shows.FirstAsync();
            Assert.True(updatedShow.HasEnded);
        }

        [Fact]
        public async Task StartAsync_Should_Create_Timer_And_Run_Periodically()
        {
            // Arrange
            using var context = new AppDbContext(_dbOptions);
            var mockLogger = new Mock<ILogger<ShowStatusService>>();
            var mockRoomService = new Mock<RoomService>(null);
            var showService = new ShowService(context, mockRoomService.Object);
            var provider = BuildServiceProvider(context, showService);
            var service = new ShowStatusService(mockLogger.Object, provider);

            // Act
            await service.StartAsync(CancellationToken.None);

            // Assert
            mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString().Contains("wird gestartet")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);

            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task StopAsync_Should_Disable_Timer()
        {
            // Arrange
            using var context = new AppDbContext(_dbOptions);
            var mockLogger = new Mock<ILogger<ShowStatusService>>();
            var mockRoomService = new Mock<RoomService>(null);
            var showService = new ShowService(context, mockRoomService.Object);
            var provider = BuildServiceProvider(context, showService);
            var service = new ShowStatusService(mockLogger.Object, provider);

            await service.StartAsync(CancellationToken.None);

            // Act
            await service.StopAsync(CancellationToken.None);

            // Assert
            mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString().Contains("wird gestoppt")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        }
    }
}
