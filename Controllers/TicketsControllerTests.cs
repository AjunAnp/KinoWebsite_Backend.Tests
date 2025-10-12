using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinoWebsite_Backend.Controllers;
using KinoWebsite_Backend.Services;
using KinoWebsite_Backend.Models;
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

        private async Task<Show> AddTestShowAsync()
        {
            var movie = new Movie
            {
                Title = "Test Movie",
                Genre = "Action",
                Description = "Some movie",
                Duration = 120,
                ReleaseDate = DateTime.UtcNow,
                TrailerUrl = "url",
                Director = "Someone",
                ImDbRating = 7.5,
                Cast = new[] { "Actor" },
                ImageUrl = "img",
                AgeRestriction = AgeRestriction.UsK12
            };

            var room = new Room { Name = "Room 1", isAvailable = true };

            var show = new Show
            {
                Movie = movie,
                Room = room,
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddHours(2),
                Language = "DE",
                Subtitle = "EN",
                BasePrice = 10.0
            };

            _context.Shows.Add(show);
            await _context.SaveChangesAsync();
            return show;
        }

        private async Task<Seat> AddTestSeatAsync(Room room)
        {
            var seat = new Seat
            {
                Room = room,
                Row = 'A',
                SeatNumber = 1,
                Type = "Standard",
                isAvailable = true
            };
            _context.Seats.Add(seat);
            await _context.SaveChangesAsync();
            return seat;
        }

        [Fact]
        public async Task GetTickets_ReturnsOk_WithTickets()
        {
            var show = await AddTestShowAsync();
            var seat = await AddTestSeatAsync(show.Room);

            _context.Tickets.AddRange(
                new Ticket { Show = show, Seat = seat, SeatType = seat.Type, TicketState = TicketStates.Reserved, Price = 12 },
                new Ticket { Show = show, Seat = seat, SeatType = seat.Type, TicketState = TicketStates.Available, Price = 10 }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetTickets();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var tickets = Assert.IsAssignableFrom<IEnumerable<TicketDto>>(ok.Value);
            Assert.Equal(2, tickets.Count());
        }

        [Fact]
        public async Task GetTicket_ReturnsNotFound_WhenMissing()
        {
            var result = await _controller.GetTicket(999);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetTicket_ReturnsOk_WhenExists()
        {
            var show = await AddTestShowAsync();
            var seat = await AddTestSeatAsync(show.Room);

            var ticket = new Ticket
            {
                Show = show,
                Seat = seat,
                SeatType = seat.Type,
                Price = 15,
                TicketState = TicketStates.Booked
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            var result = await _controller.GetTicket(ticket.Id);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<TicketDetailDto>(ok.Value);
            Assert.Equal(ticket.Price, dto.Price);
            Assert.Equal("Booked", dto.TicketState);
        }

        [Fact]
        public async Task DeleteTicket_RemovesTicket_WhenExists()
        {
            var show = await AddTestShowAsync();
            var seat = await AddTestSeatAsync(show.Room);

            var ticket = new Ticket
            {
                Show = show,
                Seat = seat,
                SeatType = seat.Type,
                Price = 10,
                TicketState = TicketStates.Available
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteTicket(ticket.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_context.Tickets);
        }

        [Fact]
        public async Task DeleteTicket_ReturnsNotFound_WhenMissing()
        {
            var result = await _controller.DeleteTicket(99);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetTicketQrCode_ReturnsOk_WithQrCode()
        {
            var show = await AddTestShowAsync();
            var seat = await AddTestSeatAsync(show.Room);

            var ticket = new Ticket
            {
                Show = show,
                Seat = seat,
                SeatType = seat.Type,
                Price = 15,
                TicketState = TicketStates.Available
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            var result = await _controller.GetTicketQrCode(ticket.Id);

            var ok = Assert.IsType<OkObjectResult>(result);

            var qrCodeProp = ok.Value.GetType().GetProperty("QrCode");
            Assert.NotNull(qrCodeProp);

            var qrValue = qrCodeProp.GetValue(ok.Value)?.ToString();
            Assert.NotNull(qrValue);
            Assert.StartsWith("data:image/png;base64,", qrValue);
        }

        [Fact]
        public async Task GetTicketsByUserId_ReturnsOk_WhenTicketsExist()
        {
            var show = await AddTestShowAsync();
            var seat = await AddTestSeatAsync(show.Room);
            var user = new User
            {
                Firstname = "Alice",
                Lastname = "User",
                Email = "alice@example.com",
                Password = "pwd",
                PhoneNumber = "123"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var order = new Order
            {
                User = user,
                TimeOfOrder = DateTime.UtcNow,
                TotalPrice = 10
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var ticket = new Ticket
            {
                Show = show,
                Seat = seat,
                SeatType = seat.Type,
                Price = 10,
                Order = order,
                TicketState = TicketStates.Reserved
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            var result = await _controller.GetTicketsByUserId(user.Id);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<TicketDetailDto>>(ok.Value);
            Assert.Single(list);
            Assert.Equal(10, list.First().Price);
        }

        [Fact]
        public async Task GetTicketsByUserId_ReturnsOk_WhenNoTickets()
        {
            var result = await _controller.GetTicketsByUserId(999);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<TicketDetailDto>>(ok.Value);
            Assert.Empty(list);
        }
    }
}
