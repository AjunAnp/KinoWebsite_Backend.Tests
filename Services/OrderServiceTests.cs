using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.DTOs;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace KinoWebsite_Backend.Tests.Services
{
    public class OrderServiceTests
    {
        private (OrderService service, AppDbContext context) CreateService()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var context = new AppDbContext(options);

            var emailMock = new Mock<IEmailService>();
            emailMock.Setup(e => e.SendEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<System.Net.Mail.LinkedResource>>()))
                .Returns(Task.CompletedTask);

            var payPalMock = new Mock<IPayPalService>();
            payPalMock.Setup(p => p.CreateOrderAsync(It.IsAny<string>(), It.IsAny<string>()))
                      .ReturnsAsync("PAYPAL123");

            var ticketService = new TicketService(context);
            var service = new OrderService(context, emailMock.Object, ticketService, payPalMock.Object);

            return (service, context);
        }

        private User CreateUser(string name = "TestUser") => new User
        {
            Firstname = name,
            Lastname = "Tester",
            Email = $"{name.ToLower()}@example.com",
            Password = "hashed",
            PhoneNumber = "123456"
        };

        private Movie CreateMovie(string title = "Film") => new Movie
        {
            Title = title,
            Genre = "Action",
            Description = "Beschreibung",
            Duration = 120,
            ReleaseDate = DateTime.UtcNow,
            Director = "Director",
            ImageUrl = "",
            TrailerUrl = "",
            ImDbRating = 7.5,
            AgeRestriction = AgeRestriction.UsK12,
            Cast = Array.Empty<string>()
        };

        [Fact]
        public async Task CreateAsync_CreatesOrder_WithTickets()
        {
            var (service, db) = CreateService();

            var user = CreateUser();
            var movie = CreateMovie();
            var room = new Room { Name = "Saal 1", Capacity = 50, isAvailable = true };

            var show = new Show
            {
                Room = room,
                Movie = movie,
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddHours(2),
                Language = "Deutsch",
                Subtitle = "Englisch",
                BasePrice = 10
            };

            var seat = new Seat
            {
                Room = room,
                Row = 'A',
                SeatNumber = 1,
                Type = "Standard",
                isAvailable = true
            };

            db.Users.Add(user);
            db.Movies.Add(movie);
            db.Rooms.Add(room);
            db.Shows.Add(show);
            db.Seats.Add(seat);
            await db.SaveChangesAsync();

            var dto = new OrderCreateDto
            {
                UserId = user.Id,
                ShowId = show.Id,
                SeatIds = new List<int> { seat.Id }
            };

            var order = await service.CreateAsync(dto);

            Assert.NotNull(order);
            Assert.Single(order.Tickets);
            Assert.Equal(user.Id, order.UserId);
            Assert.True(order.TotalPrice > 0);
        }

        [Fact]
        public async Task CreateAsync_Throws_WhenSeatAlreadyBooked()
        {
            var (service, db) = CreateService();

            var user = CreateUser();
            var movie = CreateMovie();
            var room = new Room { Name = "Saal 2", Capacity = 100, isAvailable = true };

            var show = new Show
            {
                Room = room,
                Movie = movie,
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddHours(3),
                Language = "Deutsch",
                Subtitle = "Englisch",
                BasePrice = 10
            };

            var seat = new Seat
            {
                Room = room,
                Row = 'B',
                SeatNumber = 5,
                Type = "Standard",
                isAvailable = true
            };

            db.Users.Add(user);
            db.Rooms.Add(room);
            db.Movies.Add(movie);
            db.Shows.Add(show);
            db.Seats.Add(seat);
            await db.SaveChangesAsync();

            db.Tickets.Add(new Ticket
            {
                ShowId = show.Id,
                SeatId = seat.Id,
                TicketState = TicketStates.Reserved,
                SeatType = seat.Type
            });
            await db.SaveChangesAsync();

            var dto = new OrderCreateDto
            {
                UserId = user.Id,
                ShowId = show.Id,
                SeatIds = new List<int> { seat.Id }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto));
        }

        [Fact]
        public async Task DeleteAsync_RemovesOrder()
        {
            var (service, db) = CreateService();

            var user = CreateUser();
            db.Users.Add(user);

            var order = new Order
            {
                User = user,
                UserId = user.Id,
                TotalPrice = 20
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var result = await service.DeleteAsync(order.Id);

            Assert.True(result);
            Assert.Empty(db.Orders);
        }

        [Fact]
        public async Task ApplyDiscountAsync_AppliesValidDiscount()
        {
            var (service, db) = CreateService();

            var discount = new Discount
            {
                Code = "SAVE10",
                Percentage = 10,
                ValidUntil = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };
            db.Discounts.Add(discount);

            var user = CreateUser();
            db.Users.Add(user);

            var order = new Order
            {
                User = user,
                UserId = user.Id,
                Tickets = new List<Ticket> { new Ticket { Price = 100, SeatType = "Standard", TicketState = TicketStates.Available } },
                TotalPrice = 100
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var result = await service.ApplyDiscountAsync(order.Id, "SAVE10");

            Assert.NotNull(result);
            Assert.True(result.TotalPrice < 100);
            Assert.Equal(discount.Id, result.DiscountId);
        }

        [Fact]
        public async Task CheckTransactionAsync_SendsEmail_WhenOrderExists()
        {
            var (service, db) = CreateService();

            var user = CreateUser();
            db.Users.Add(user);

            var order = new Order
            {
                User = user,
                UserId = user.Id,
                Tickets = new List<Ticket>
                {
                    new Ticket
                    {
                        Price = 15,
                        TicketState = TicketStates.Booked,
                        SeatType = "Standard"
                    }
                },
                TotalPrice = 15
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var result = await service.CheckTransactionAsync(order.Id);

            Assert.True(result);
        }
    }
}
