using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using KinoWebsite_Backend.Controllers;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using KinoWebsite_Backend.DTOs;
using KinoWebsite_Backend.Data;

namespace KinoWebsite_Backend.Tests.Controllers
{
    public class TicketsControllerTests
    {
        private readonly AppDbContext _context;
        private readonly TicketService _service;
        private readonly TicketsController _controller;

        public TicketsControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _service = new TicketService(_context);
            _controller = new TicketsController(_service);
        }

        // ----------- Helper Setup -----------

        private Movie CreateMovie(string title = "Inception")
        {
            var movie = new Movie
            {
                Title = title,
                Genre = "Sci-Fi",
                Duration = 120,
                Description = "Dream layers",
                Director = "Christopher Nolan",
                ImageUrl = "http://example.com/img.jpg",
                TrailerUrl = "http://example.com/trailer.mp4",
                ReleaseDate = DateTime.UtcNow,
                ImDbRating = 8.8,
                Cast = Array.Empty<string>(),
                AgeRestriction = AgeRestriction.UsK12
            };
            _context.Movies.Add(movie);
            _context.SaveChanges();
            return movie;
        }

        private Room CreateRoom(string name = "Saal 1")
        {
            var room = new Room { Name = name, Capacity = 100, isAvailable = true };
            _context.Rooms.Add(room);
            _context.SaveChanges();
            return room;
        }

        private Show CreateShow(Movie movie, Room room)
        {
            var show = new Show
            {
                Language = "EN",
                Is3D = false,
                Subtitle = "none",
                FreeSeats = 100,
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddHours(2),
                MovieId = movie.Id,
                RoomId = room.Id
            };
            _context.Shows.Add(show);
            _context.SaveChanges();
            return show;
        }

        private Seat CreateSeat(Room room, char row = 'A', int seatNumber = 1)
        {
            var seat = new Seat
            {
                RoomId = room.Id,
                Row = row,
                SeatNumber = seatNumber,
                Type = "Standard",
                isAvailable = true
            };
            _context.Seats.Add(seat);
            _context.SaveChanges();
            return seat;
        }

        private Order CreateOrder(int userId = 1)
        {
            var order = new Order { UserId = userId };
            _context.Orders.Add(order);
            _context.SaveChanges();
            return order;
        }

        private Ticket CreateTicket()
        {
            var movie = CreateMovie();
            var room = CreateRoom();
            var show = CreateShow(movie, room);
            var seat = CreateSeat(room);
            var order = CreateOrder();

            var ticket = new Ticket
            {
                ShowId = show.Id,
                SeatId = seat.Id,
                OrderId = order.Id,
                Price = 15.0,
                TicketState = TicketStates.Booked
            };

            _context.Tickets.Add(ticket);
            _context.SaveChanges();
            return ticket;
        }

        // ----------- TESTS -----------

        [Fact]
        public async Task GetTickets_ReturnsOk_WithTickets()
        {
            // Arrange
            CreateTicket();

            // Act
            var result = await _controller.GetTickets();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<TicketDto>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetTicket_ReturnsNotFound_WhenMissing()
        {
            // Act
            var result = await _controller.GetTicket(999);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetTicket_ReturnsOk_WhenExists()
        {
            // Arrange
            var ticket = CreateTicket();

            // Act
            var result = await _controller.GetTicket(ticket.Id);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<TicketDetailDto>(ok.Value);
            Assert.Equal(ticket.Id, dto.Id);
        }

        [Fact]
        public async Task GetTicketQrCode_ReturnsBase64_WhenTicketExists()
        {
            // Arrange
            var ticket = CreateTicket();

            // Act
            var result = await _controller.GetTicketQrCode(ticket.Id);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);

            // FIX: Controller gibt ein anonymes Objekt zurück: { QrCode = "..." }
            var qrData = ok.Value?.GetType().GetProperty("QrCode")?.GetValue(ok.Value, null)?.ToString();

            Assert.NotNull(qrData);
            Assert.StartsWith("data:image/png;base64,", qrData);
        }

        [Fact]
        public async Task GetTicketQrCode_ReturnsNotFound_WhenMissing()
        {
            // Act
            var result = await _controller.GetTicketQrCode(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteTicket_Removes_WhenExists()
        {
            // Arrange
            var ticket = CreateTicket();

            // Act
            var result = await _controller.DeleteTicket(ticket.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_context.Tickets);
        }

        [Fact]
        public async Task DeleteTicket_ReturnsNotFound_WhenMissing()
        {
            // Act
            var result = await _controller.DeleteTicket(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetTicketsByUserId_ReturnsOk_WhenUserHasTickets()
        {
            // Arrange
            var ticket = CreateTicket();
            var userId = _context.Orders.First().UserId;

            // Act
            var result = await _controller.GetTicketsByUserId(userId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<TicketDetailDto>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetTicketsByUserId_ReturnsOk_WhenUserHasNone()
        {
            // Act
            var result = await _controller.GetTicketsByUserId(999);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<TicketDetailDto>>(ok.Value);
            Assert.Empty(list);
        }

    }
}
