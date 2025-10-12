using Xunit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using KinoWebsite_Backend.Services;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Data;

namespace KinoWebsite_Backend.Tests.Services
{
    public class TicketServiceTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private async Task<(Show show, Seat seat)> CreateShowAndSeatAsync(AppDbContext db)
        {
            var movie = new Movie
            {
                Title = "Testfilm",
                Genre = "Action",
                Description = "Test",
                Duration = 120,
                ReleaseDate = DateTime.UtcNow,
                TrailerUrl = "url",
                Director = "Dir",
                ImDbRating = 8.0,
                Cast = new[] { "Actor" },
                ImageUrl = "img",
                AgeRestriction = AgeRestriction.UsK12
            };

            var room = new Room { Name = "Saal 1", isAvailable = true };
            var show = new Show
            {
                Movie = movie,
                Room = room,
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddHours(2),
                Language = "DE",
                Subtitle = "EN",
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

            db.Shows.Add(show);
            db.Seats.Add(seat);
            await db.SaveChangesAsync();

            return (show, seat);
        }

        [Fact]
        public async Task GetAllTicketsAsync_ShouldReturnAllTickets()
        {
            var db = GetDbContext();
            var service = new TicketService(db);
            var (show, seat) = await CreateShowAndSeatAsync(db);

            db.Tickets.Add(new Ticket
            {
                Show = show,
                Seat = seat,
                SeatType = seat.Type,
                Price = 10,
                TicketState = TicketStates.Available
            });
            db.Tickets.Add(new Ticket
            {
                Show = show,
                Seat = seat,
                SeatType = seat.Type,
                Price = 12,
                TicketState = TicketStates.Reserved
            });
            await db.SaveChangesAsync();

            var result = await service.GetAllTicketsAsync();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetTicketByIdAsync_ShouldReturnTicket_WhenExists()
        {
            var db = GetDbContext();
            var service = new TicketService(db);
            var (show, seat) = await CreateShowAndSeatAsync(db);

            var ticket = new Ticket
            {
                Show = show,
                Seat = seat,
                SeatType = seat.Type,
                Price = 14.5,
                TicketState = TicketStates.Booked
            };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();

            var found = await service.GetTicketByIdAsync(ticket.Id);

            Assert.NotNull(found);
            Assert.Equal(TicketStates.Booked, found.TicketState);
        }

        [Fact]
        public async Task GetTicketByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            var db = GetDbContext();
            var service = new TicketService(db);

            var result = await service.GetTicketByIdAsync(99);

            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteTicketAsync_ShouldRemoveTicket()
        {
            var db = GetDbContext();
            var service = new TicketService(db);
            var (show, seat) = await CreateShowAndSeatAsync(db);

            var ticket = new Ticket
            {
                Show = show,
                Seat = seat,
                SeatType = seat.Type,
                Price = 10,
                TicketState = TicketStates.Reserved
            };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();

            var success = await service.DeleteTicketAsync(ticket.Id);

            Assert.True(success);
            Assert.Empty(db.Tickets);
        }

        [Fact]
        public async Task DeleteTicketAsync_ShouldReturnFalse_WhenMissing()
        {
            var db = GetDbContext();
            var service = new TicketService(db);

            var result = await service.DeleteTicketAsync(999);

            Assert.False(result);
        }

        [Fact]
        public void GenerateQrCode_ShouldReturnBase64String_WhenDetailsValid()
        {
            var db = GetDbContext();
            var service = new TicketService(db);

            var movie = new Movie
            {
                Title = "Avatar",
                Genre = "Sci-Fi",
                Description = "Blue",
                Duration = 120,
                ReleaseDate = DateTime.UtcNow,
                TrailerUrl = "url",
                Director = "James",
                ImDbRating = 8.0,
                Cast = new[] { "Actor" },
                ImageUrl = "img",
                AgeRestriction = AgeRestriction.UsK12
            };
            var room = new Room { Name = "Room", isAvailable = true };
            var show = new Show
            {
                Movie = movie,
                Room = room,
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddHours(2),
                Language = "DE",
                Subtitle = "EN"
            };
            var seat = new Seat
            {
                Room = room,
                Row = 'B',
                SeatNumber = 5,
                Type = "Standard",
                isAvailable = true
            };
            var ticket = new Ticket
            {
                Id = 1,
                Show = show,
                Seat = seat,
                SeatType = seat.Type,
                Price = 15
            };

            var qr = service.GenerateQrCode(ticket);

            Assert.StartsWith("data:image/png;base64,", qr);
        }

        [Fact]
        public void GenerateQrCode_ShouldReturnError_WhenMissingDetails()
        {
            var db = GetDbContext();
            var service = new TicketService(db);

            var ticket = new Ticket { Id = 1 };

            var qr = service.GenerateQrCode(ticket);

            Assert.Equal("Error: Ticket details missing.", qr);
        }

        [Fact]
        public async Task GetTicketsByUserIdAsync_ShouldReturnUserTickets()
        {
            var db = GetDbContext();
            var service = new TicketService(db);
            var (show, seat) = await CreateShowAndSeatAsync(db);

            var user = new User
            {
                Firstname = "Test",
                Lastname = "User",
                Email = "t@u.de",
                Password = "pw",
                PhoneNumber = "000"
            };
            var order = new Order
            {
                User = user,
                TotalPrice = 10,
                TimeOfOrder = DateTime.UtcNow
            };

            db.Users.Add(user);
            db.Orders.Add(order);
            db.Tickets.Add(new Ticket
            {
                Show = show,
                Seat = seat,
                SeatType = seat.Type,
                Order = order,
                Price = 10,
                TicketState = TicketStates.Available
            });
            await db.SaveChangesAsync();

            var result = await service.GetTicketsByUserIdAsync(user.Id);

            Assert.Single(result);
            Assert.Equal(10, result.First().Price);
        }

        [Fact]
        public async Task GetTicketsByUserIdAsync_ShouldReturnEmpty_WhenUserHasNoTickets()
        {
            var db = GetDbContext();
            var service = new TicketService(db);

            var result = await service.GetTicketsByUserIdAsync(123);

            Assert.Empty(result);
        }

        [Fact]
        public void GenerateQrCodeAsByteArray_ShouldReturnBytes_WhenValid()
        {
            var db = GetDbContext();
            var service = new TicketService(db);

            var movie = new Movie
            {
                Title = "Matrix",
                Genre = "Action",
                Description = "Neo",
                Duration = 136,
                ReleaseDate = DateTime.UtcNow,
                TrailerUrl = "url",
                Director = "Wachowski",
                ImDbRating = 8.5,
                Cast = new[] { "Keanu Reeves" },
                ImageUrl = "img",
                AgeRestriction = AgeRestriction.UsK12
            };
            var room = new Room { Name = "Room", isAvailable = true };
            var show = new Show
            {
                Movie = movie,
                Room = room,
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddHours(2),
                Language = "DE",
                Subtitle = "EN"
            };
            var seat = new Seat
            {
                Room = room,
                Row = 'C',
                SeatNumber = 2,
                Type = "Standard",
                isAvailable = true
            };
            var ticket = new Ticket
            {
                Id = 1,
                Show = show,
                Seat = seat,
                SeatType = seat.Type,
                Price = 12
            };

            var bytes = service.GenerateQrCodeAsByteArray(ticket);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public void GenerateQrCodeAsByteArray_ShouldReturnNull_WhenMissingData()
        {
            var db = GetDbContext();
            var service = new TicketService(db);

            var ticket = new Ticket();

            var result = service.GenerateQrCodeAsByteArray(ticket);

            Assert.Null(result);
        }
    }
}
