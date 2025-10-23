using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.DTOs;
using KinoWebsite_Backend.Services;
using Moq;
using Microsoft.Extensions.Logging;

namespace KinoWebsite_Backend.Tests.Services
{
    public class ShowServiceTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<RoomService> _roomServiceMock;
        private readonly ShowService _service;

        public ShowServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)) // 👈 suppress TransactionIgnoredWarning
                .Options;

            _context = new AppDbContext(options);
            var serviceProviderMock = new Mock<IServiceProvider>();
            var roomService = new RoomService(_context, serviceProviderMock.Object);
            _service = new ShowService(_context, roomService);

        }


        private Movie CreateTestMovie()
        {
            return new Movie
            {
                Title = "Matrix",
                Description = "Testfilm",
                Director = "Test Regisseur",
                Genre = "Action",
                ImageUrl = "https://example.com/poster.jpg",
                TrailerUrl = "https://example.com/trailer.mp4",
                Cast = Array.Empty<string>()
            };
        }

        private Room CreateTestRoom()
        {
            return new Room { Name = "Saal 1" };
        }
        

        [Fact]
        public async Task CreateShowAsync_Should_Create_Show_When_Valid()
        {
            var movie = CreateTestMovie();
            var room = CreateTestRoom();
            _context.Movies.Add(movie);
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            var dto = new ShowCreateDto
            {
                Language = "DE",
                Is3D = false,
                Subtitle = "EN",
                StartUtc = DateTime.UtcNow.AddHours(1),
                EndUtc = DateTime.UtcNow.AddHours(3),
                MovieId = movie.Id,
                RoomId = room.Id,
                BasePrice = 10.5
            };

            var result = await _service.CreateShowAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(dto.MovieId, result.MovieId);
            Assert.Equal(dto.RoomId, result.RoomId);
            Assert.Equal(1, await _context.Shows.CountAsync());
        }

        [Fact]
        public async Task CreateShowAsync_Should_Throw_When_End_Before_Start()
        {
            var dto = new ShowCreateDto
            {
                Language = "DE",
                Is3D = false,
                Subtitle = "EN",
                StartUtc = DateTime.UtcNow.AddHours(3),
                EndUtc = DateTime.UtcNow.AddHours(2),
                MovieId = 1,
                RoomId = 1,
                BasePrice = 10.0
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateShowAsync(dto));
        }

        [Fact]
        public async Task CreateShowAsync_Should_Throw_When_Overlapping()
        {
            var movie = CreateTestMovie();
            var room = CreateTestRoom();
            _context.Movies.Add(movie);
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            var existing = new Show
            {
                Language = "EN",
                Subtitle = "DE",
                StartUtc = DateTime.UtcNow.AddHours(1),
                EndUtc = DateTime.UtcNow.AddHours(3),
                RoomId = room.Id,
                MovieId = movie.Id
            };
            _context.Shows.Add(existing);
            await _context.SaveChangesAsync();

            var dto = new ShowCreateDto
            {
                Language = "DE",
                Subtitle = "EN",
                StartUtc = DateTime.UtcNow.AddHours(2),
                EndUtc = DateTime.UtcNow.AddHours(4),
                MovieId = movie.Id,
                RoomId = room.Id
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateShowAsync(dto));
        }

        [Fact]
        public async Task UpdateShowAsync_Should_Update_Existing_Show()
        {
            var show = new Show
            {
                Language = "EN",
                Subtitle = "None",
                Is3D = false,
                StartUtc = DateTime.UtcNow.AddHours(1),
                EndUtc = DateTime.UtcNow.AddHours(2),
                MovieId = 1,
                RoomId = 1,
                BasePrice = 10
            };
            _context.Shows.Add(show);
            await _context.SaveChangesAsync();

            var dto = new ShowUpdateDto
            {
                Language = "DE",
                Is3D = true,
                Subtitle = "EN",
                StartUtc = DateTime.UtcNow.AddHours(1),
                EndUtc = DateTime.UtcNow.AddHours(3),
                MovieId = 1,
                RoomId = 1,
                BasePrice = 12
            };

            await _service.UpdateShowAsync(show.Id, dto);

            var updated = await _context.Shows.FindAsync(show.Id);
            Assert.Equal("DE", updated.Language);
            Assert.True(updated.Is3D);
            Assert.Equal(12, updated.BasePrice);
        }

        [Fact]
        public async Task DeleteShowAsync_Should_Remove_Show()
        {
            var show = new Show
            {
                Language = "EN",
                Subtitle = "DE",
                StartUtc = DateTime.UtcNow.AddHours(1),
                EndUtc = DateTime.UtcNow.AddHours(2),
                MovieId = 1,
                RoomId = 1
            };
            _context.Shows.Add(show);
            await _context.SaveChangesAsync();

            var result = await _service.DeleteShowAsync(show.Id);

            Assert.True(result);
            Assert.Empty(_context.Shows);
        }

        [Fact]
        public async Task StartShowAsync_Should_Invalidate_Reserved_Tickets()
        {
            var show = new Show
            {
                Language = "EN",
                Subtitle = "DE",
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddHours(2),
                MovieId = 1,
                RoomId = 1
            };
            _context.Shows.Add(show);
            await _context.SaveChangesAsync();

            var ticket = new Ticket
            {
                ShowId = show.Id,
                TicketState = TicketStates.Reserved,
                SeatType = "Standard",
                SeatId = 1
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            await _service.StartShowAsync(show.Id);

            var updatedTicket = await _context.Tickets.FindAsync(ticket.Id);
            Assert.Equal(TicketStates.Invalid, updatedTicket.TicketState);
        }
    }
}
