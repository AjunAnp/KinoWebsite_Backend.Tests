using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using KinoWebsite_Backend.Controllers;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.DTOs;
using KinoWebsite_Backend.Services;
using KinoWebsite_Backend.Data;

namespace KinoWebsite_Backend.Tests.Controllers
{
    public class SeatsControllerTests
    {
        private readonly AppDbContext _context;
        private readonly SeatService _service;
        private readonly SeatsController _controller;

        public SeatsControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _service = new SeatService(_context);
            _controller = new SeatsController(_service);
        }

        private Room CreateRoom(string name = "Saal 1")
        {
            var room = new Room { Name = name, Capacity = 0, isAvailable = true };
            _context.Rooms.Add(room);
            _context.SaveChanges();
            return room;
        }

        [Fact]
        public async Task GetSeats_ReturnsAllSeats()
        {
            var room = CreateRoom();
            _context.Seats.AddRange(
                new Seat { RoomId = room.Id, Row = 'A', SeatNumber = 1, Type = "Standard", isAvailable = true },
                new Seat { RoomId = room.Id, Row = 'A', SeatNumber = 2, Type = "Standard", isAvailable = true }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetSeats();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<SeatDto>>(ok.Value);
            Assert.Equal(2, list.Count());
        }

        [Fact]
        public async Task GetSeat_ReturnsNotFound_WhenMissing()
        {
            var result = await _controller.GetSeat(999);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetSeat_ReturnsOk_WhenExists()
        {
            var room = CreateRoom();
            var seat = new Seat { RoomId = room.Id, Row = 'B', SeatNumber = 5, Type = "Premium", isAvailable = true };
            _context.Seats.Add(seat);
            await _context.SaveChangesAsync();

            var result = await _controller.GetSeat(seat.Id);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<SeatDto>(ok.Value);
            Assert.Equal('B', dto.Row);
        }

        [Fact]
        public async Task CreateSeat_ReturnsCreated_WhenValid()
        {
            var room = CreateRoom();
            var dto = new CreateSeatDto
            {
                RoomId = room.Id,
                Row = 'A',
                SeatNumber = 3,
                Type = "Standard",
                isAvailable = true
            };

            var result = await _controller.CreateSeat(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var val = Assert.IsType<SeatDto>(created.Value);
            Assert.Equal(3, val.SeatNumber);
        }

        [Fact]
        public async Task CreateSeat_ReturnsBadRequest_WhenRoomMissing()
        {
            var dto = new CreateSeatDto { RoomId = 999, Row = 'C', SeatNumber = 1, Type = "Standard", isAvailable = true };
            var result = await _controller.CreateSeat(dto);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateSeat_ChangesSeat_WhenExists()
        {
            var room = CreateRoom();
            var seat = new Seat { RoomId = room.Id, Row = 'C', SeatNumber = 2, Type = "Standard", isAvailable = true };
            _context.Seats.Add(seat);
            await _context.SaveChangesAsync();

            var dto = new UpdateSeatDto { Row = 'C', SeatNumber = 2, Type = "VIP", isAvailable = false };

            var result = await _controller.UpdateSeat(seat.Id, dto);

            Assert.IsType<NoContentResult>(result);
            var updated = await _context.Seats.FindAsync(seat.Id);
            Assert.Equal("VIP", updated.Type);
        }

        [Fact]
        public async Task DeleteSeat_RemovesSeat_WhenExists()
        {
            var room = CreateRoom();
            var seat = new Seat { RoomId = room.Id, Row = 'A', SeatNumber = 1, Type = "Standard", isAvailable = true };
            _context.Seats.Add(seat);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteSeat(seat.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_context.Seats);
        }

        [Fact]
        public async Task DeleteSeat_ReturnsNotFound_WhenMissing()
        {
            var result = await _controller.DeleteSeat(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetRowAvailability_UpdatesAllSeatsInRow()
        {
            var room = CreateRoom();
            _context.Seats.AddRange(
                new Seat { RoomId = room.Id, Row = 'A', SeatNumber = 1, Type = "Standard", isAvailable = true },
                new Seat { RoomId = room.Id, Row = 'A', SeatNumber = 2, Type = "Standard", isAvailable = true }
            );
            await _context.SaveChangesAsync();

            var dto = new AvailabilityPatchDto { isAvailable = false };

            var result = await _controller.SetRowAvailability(room.Id, 'A', dto);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(2, ok.Value); // two seats updated
        }
    }
}
