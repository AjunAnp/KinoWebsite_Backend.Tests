using System;
using System.Threading.Tasks;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace KinoWebsite_Backend.Tests.Services
{
    public class TicketServiceTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetAllTicketsAsync_ShouldReturnAllTickets()
        {
            var db = GetDbContext();
            var service = new TicketService(db);

            var movie = new Movie
            {
                Title = "Test",
                Duration = 100,
                Genre = "Action",
                Description = "Beschreibung",
                ReleaseDate = DateTime.UtcNow,
                TrailerUrl = "",
                Director = "Director",
                ImDbRating = 7.5,
                Cast = Array.Empty<string>(), 
                ImageUrl = "",
                AgeRestriction = AgeRestriction.UsK12
            };

            var room = new Room { Name = "Saal 1", isAvailable = true };
            var show = new Show
            {
                Movie = movie,
                Room = room,
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddHours(2),
                Language = "Deutsch",  
                Subtitle = "Englisch"  
            };

            var seat = new Seat { Room = room, Row = 'A', SeatNumber = 1, Type = "Standard", isAvailable = true };

            db.Tickets.Add(new Ticket { Show = show, Seat = seat, Price = 10, TicketState = TicketStates.Reserved });
            await db.SaveChangesAsync();

            var result = await service.GetAllTicketsAsync();

            Assert.Single(result);
        }

        [Fact]
        public async Task GetTicketByIdAsync_ShouldReturnTicket()
        {
            var db = GetDbContext();
            var service = new TicketService(db);

            var movie = new Movie
            {
                Title = "Matrix",
                Duration = 120,
                Genre = "Sci-Fi",
                Description = "Beschreibung",
                ReleaseDate = DateTime.UtcNow,
                TrailerUrl = "",
                Director = "Wachowski",
                ImDbRating = 8.7,
                Cast = Array.Empty<string>(), 
                ImageUrl = "",
                AgeRestriction = AgeRestriction.UsK12
            };

            var room = new Room { Name = "Saal 2", isAvailable = true };
            var show = new Show
            {
                Movie = movie,
                Room = room,
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddHours(2),
                Language = "Deutsch",  
                Subtitle = "Englisch"  
            };

            var seat = new Seat { Room = room, Row = 'B', SeatNumber = 2, Type = "Standard", isAvailable = true };
            var ticket = new Ticket { Show = show, Seat = seat, Price = 12.5, TicketState = TicketStates.Available };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();

            var found = await service.GetTicketByIdAsync(ticket.Id);

            Assert.NotNull(found);
            Assert.Equal("Matrix", found.Show.Movie.Title);
        }

        [Fact]
        public async Task DeleteTicketAsync_ShouldRemoveTicket()
        {
            var db = GetDbContext();
            var service = new TicketService(db);

            var ticket = new Ticket { Price = 5, TicketState = TicketStates.Available };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();

            var result = await service.DeleteTicketAsync(ticket.Id);

            Assert.True(result);
            Assert.Empty(db.Tickets);
        }

        [Fact]
        public void GenerateQrCode_ShouldReturnBase64String()
        {
            var db = GetDbContext();
            var service = new TicketService(db);

            var movie = new Movie
            {
                Title = "Avatar",
                Duration = 180,
                Genre = "Sci-Fi",
                Description = "Beschreibung",
                ReleaseDate = DateTime.UtcNow,
                TrailerUrl = "",
                Director = "James Cameron",
                ImDbRating = 8.0,
                Cast = Array.Empty<string>(),
                ImageUrl = "",
                AgeRestriction = AgeRestriction.UsK12
            };

            var room = new Room { Name = "Saal 5", isAvailable = true };
            var show = new Show
            {
                Movie = movie,
                Room = room,
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddHours(3),
                Language = "Deutsch", 
                Subtitle = "Englisch"  
            };

            var seat = new Seat { Room = room, Row = 'C', SeatNumber = 10, Type = "Standard", isAvailable = true };
            var ticket = new Ticket { Id = 1, Show = show, Seat = seat, Price = 15 };

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
    }
}
