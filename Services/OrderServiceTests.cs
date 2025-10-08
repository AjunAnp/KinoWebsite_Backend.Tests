using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.DTOs;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KinoWebsite_Backend.Tests.Services
{
    public class OrderServiceTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddOrder_WhenSeatsAvailable()
        {
            var db = GetDbContext();
            var service = new OrderService(db);

            var orderDto = new OrderCreateDto
            {
                UserId = 1,
                ShowId = 1,
                SeatIds = new List<int> { 1, 2 }
            };

            var order = await service.CreateAsync(orderDto);

            Assert.NotNull(order);
            Assert.Equal(2, order.Tickets.Count);
            Assert.Single(db.Orders);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenSeatAlreadyTaken()
        {
            var db = GetDbContext();
            var service = new OrderService(db);

            // Erstes Ticket belegen
            db.Tickets.Add(new Ticket { ShowId = 1, SeatId = 1, TicketState = TicketStates.Reserved });
            await db.SaveChangesAsync();

            var dto = new OrderCreateDto
            {
                UserId = 1,
                ShowId = 1,
                SeatIds = new List<int> { 1 }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveOrder()
        {
            var db = GetDbContext();
            var service = new OrderService(db);

            var order = new Order { UserId = 1, Tickets = new List<Ticket>() };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var result = await service.DeleteAsync(order.Id);

            Assert.True(result);
            Assert.Empty(db.Orders);
        }

        [Fact]
        public async Task ApplyDiscountAsync_ShouldApplyValidDiscount()
        {
            var db = GetDbContext();
            var service = new OrderService(db);

            var discount = new Discount { Code = "DISC10", Percentage = 10, ValidUntil = DateTime.UtcNow.AddDays(1), IsActive = true };
            db.Discounts.Add(discount);

            var order = new Order
            {
                UserId = 1,
                Tickets = new List<Ticket> { new Ticket { Price = 100, TicketState = TicketStates.Reserved } },
                TotalPrice = 100
            };
            db.Orders.Add(order);

            await db.SaveChangesAsync();

            var updated = await service.ApplyDiscountAsync(order.Id, "DISC10");

            Assert.NotNull(updated);
            Assert.Equal(90, updated.TotalPrice); // 10% Rabatt
        }
    }
}
