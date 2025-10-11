using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.DTOs;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace KinoWebsite_Backend.Tests.Services
{
    public class OrderServiceTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        private OrderService GetService(AppDbContext db)
        {
            var emailMock = new Mock<IEmailService>();
            var ticketMock = new Mock<TicketService>(db);
            var paypalMock = new Mock<IPayPalService>();
            return new OrderService(db, emailMock.Object, ticketMock.Object, paypalMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddOrder_WhenSeatsAvailable()
        {
            // Arrange
            var db = GetDbContext();
            var service = GetService(db);

            var dto = new OrderCreateDto
            {
                UserId = 1,
                ShowId = 1,
                SeatIds = new List<int> { 1, 2 }
            };

            // Act
            var order = await service.CreateAsync(dto);

            // Assert
            Assert.NotNull(order);
            Assert.Equal(2, order.Tickets.Count);
            Assert.Single(db.Orders);
            Assert.InRange(order.TotalPrice, 24.9, 25.1);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenSeatAlreadyTaken()
        {
            // Arrange
            var db = GetDbContext();
            var service = GetService(db);

            db.Tickets.Add(new Ticket
            {
                ShowId = 1,
                SeatId = 1,
                TicketState = TicketStates.Reserved
            });
            await db.SaveChangesAsync();

            var dto = new OrderCreateDto
            {
                UserId = 1,
                ShowId = 1,
                SeatIds = new List<int> { 1 }
            };

            // Act + Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveOrder_WhenExists()
        {
            // Arrange
            var db = GetDbContext();
            var service = GetService(db);

            var order = new Order { UserId = 1, Tickets = new List<Ticket>() };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            // Act
            var result = await service.DeleteAsync(order.Id);

            // Assert
            Assert.True(result);
            Assert.Empty(db.Orders);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenNotFound()
        {
            var db = GetDbContext();
            var service = GetService(db);

            var result = await service.DeleteAsync(999);
            Assert.False(result);
        }

        [Fact]
        public async Task ApplyDiscountAsync_ShouldApplyValidDiscount()
        {
            // Arrange
            var db = GetDbContext();
            var service = GetService(db);

            var discount = new Discount
            {
                Code = "DISC10",
                Percentage = 10,
                ValidUntil = DateTime.UtcNow.AddDays(1),
                IsActive = true
            };
            db.Discounts.Add(discount);

            var order = new Order
            {
                UserId = 1,
                Tickets = new List<Ticket>
                {
                    new Ticket { Price = 100, TicketState = TicketStates.Reserved }
                },
                TotalPrice = 100
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            // Act
            var updated = await service.ApplyDiscountAsync(order.Id, "DISC10");

            // Assert
            Assert.NotNull(updated);
            Assert.InRange(updated.TotalPrice, 89.9, 90.1);
            Assert.Equal(discount.Id, updated.DiscountId);
        }

        [Fact]
        public async Task ApplyDiscountAsync_ShouldThrow_WhenInvalidDiscount()
        {
            var db = GetDbContext();
            var service = GetService(db);

            var order = new Order
            {
                UserId = 1,
                Tickets = new List<Ticket> { new Ticket { Price = 50 } },
                TotalPrice = 50
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ApplyDiscountAsync(order.Id, "INVALID"));
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnOrder_WhenExists()
        {
            var db = GetDbContext();
            var service = GetService(db);

            var order = new Order { UserId = 1, Tickets = new List<Ticket>() };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var found = await service.GetByIdAsync(order.Id);

            Assert.NotNull(found);
            Assert.Equal(order.Id, found.Id);
        }
    }
}
