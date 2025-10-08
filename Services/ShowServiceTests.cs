using System;
using System.Threading.Tasks;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KinoWebsite_Backend.Tests.Services
{
    public class ShowServiceTests
    {
        private (AppDbContext, ShowService) GetService()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new AppDbContext(options);
            var roomService = new RoomService(db);
            return (db, new ShowService(db, roomService));
        }

        [Fact]
        public async Task CreateShowAsync_ShouldAddShow()
        {
            var (db, service) = GetService();
            var movie = new Movie { Title = "Inception" };
            var room = new Room { Name = "Saal 1" };
            db.Movies.Add(movie);
            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            var show = new Show
            {
                MovieId = movie.Id,
                RoomId = room.Id,
                Language = "DE",
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddHours(2)
            };

            var created = await service.CreateShowAsync(show);

            Assert.NotNull(created);
            Assert.Single(db.Shows);
        }

        [Fact]
        public async Task CreateShowAsync_ShouldThrow_WhenEndBeforeStart()
        {
            var (db, service) = GetService();
            var movie = new Movie { Title = "Bad Show" };
            var room = new Room { Name = "Saal 2" };
            db.Movies.Add(movie);
            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            var show = new Show
            {
                MovieId = movie.Id,
                RoomId = room.Id,
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddMinutes(-10)
            };

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateShowAsync(show));
        }

        [Fact]
        public async Task UpdateShowAsync_ShouldModifyShow()
        {
            var (db, service) = GetService();
            var movie = new Movie { Title = "Old" };
            var room = new Room { Name = "Saal 3" };
            db.Movies.Add(movie);
            db.Rooms.Add(room);
            var show = new Show { Movie = movie, Room = room, StartUtc = DateTime.UtcNow, EndUtc = DateTime.UtcNow.AddHours(2) };
            db.Shows.Add(show);
            await db.SaveChangesAsync();

            show.Language = "EN";
            show.EndUtc = show.StartUtc.AddHours(3);

            var result = await service.UpdateShowAsync(show.Id, show);

            Assert.True(result);
            var refreshed = await db.Shows.FindAsync(show.Id);
            Assert.Equal("EN", refreshed.Language);
        }

        [Fact]
        public async Task DeleteShowAsync_ShouldRemoveShow()
        {
            var (db, service) = GetService();
            var show = new Show { StartUtc = DateTime.UtcNow, EndUtc = DateTime.UtcNow.AddHours(1) };
            db.Shows.Add(show);
            await db.SaveChangesAsync();

            var result = await service.DeleteShowAsync(show.Id);

            Assert.True(result);
            Assert.Empty(db.Shows);
        }

        [Fact]
        public async Task StartShowAsync_ShouldInvalidateReservedTickets()
        {
            var (db, service) = GetService();
            var show = new Show { StartUtc = DateTime.UtcNow, EndUtc = DateTime.UtcNow.AddHours(1) };
            db.Shows.Add(show);
            db.Tickets.Add(new Ticket { Show = show, TicketState = TicketStates.Reserved });
            await db.SaveChangesAsync();

            var result = await service.StartShowAsync(show.Id);

            Assert.True(result);
            var ticket = await db.Tickets.FirstAsync();
            Assert.Equal(TicketStates.Invalid, ticket.TicketState);
        }

        [Fact]
        public async Task GetShowByIdAsync_ShouldReturnShow()
        {
            var (db, service) = GetService();
            var show = new Show { Language = "DE", StartUtc = DateTime.UtcNow, EndUtc = DateTime.UtcNow.AddHours(1) };
            db.Shows.Add(show);
            await db.SaveChangesAsync();

            var found = await service.GetShowByIdAsync(show.Id);

            Assert.NotNull(found);
            Assert.Equal("DE", found.Language);
        }
    }
}
