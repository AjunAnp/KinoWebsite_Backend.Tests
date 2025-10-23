using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinoWebsite_Backend.Controllers;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.DTOs;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace KinoWebsite_Backend.Tests.Controllers
{
    public class RoomsControllerTests
    {
        private readonly AppDbContext _context;
        private readonly RoomService _roomService;
        private readonly SeatService _seatService;
        private readonly RoomsController _controller;

        public RoomsControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            var serviceProviderMock = new Mock<IServiceProvider>();

            _roomService = new RoomService(_context, serviceProviderMock.Object);
            _seatService = new SeatService(_context);
            _controller = new RoomsController(_roomService, _seatService);
        }

        [Fact]
        public async Task GetRooms_ReturnsOk_WithList()
        {
            _context.Rooms.Add(new Room { Name = "Saal 1", Capacity = 100, isAvailable = true });
            await _context.SaveChangesAsync();

            var result = await _controller.GetRooms();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetRoom_ReturnsNotFound_WhenMissing()
        {
            var result = await _controller.GetRoom(999);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetRoom_ReturnsOk_WhenExists()
        {
            var room = new Room { Name = "Saal 2", Capacity = 50, isAvailable = true };
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            var result = await _controller.GetRoom(room.Id);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<RoomDto>(ok.Value);
            Assert.Equal("Saal 2", dto.Name);
        }

        [Fact]
        public async Task CreateRoom_AddsAndReturnsCreated()
        {
            var dto = new CreateRoomDto { Name = "Neuer Saal", isAvailable = true };

            var result = await _controller.CreateRoom(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dtoResult = Assert.IsType<RoomDto>(created.Value);
            Assert.Equal("Neuer Saal", dtoResult.Name);
            Assert.Single(_context.Rooms);
        }

        [Fact]
        public async Task UpdateRoom_ChangesData_WhenExists()
        {
            var room = new Room { Name = "Alt", Capacity = 10, isAvailable = true };
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            _context.Entry(room).State = EntityState.Detached;

            var dto = new UpdateRoomDto { Name = "Neu", isAvailable = false };

            var result = await _controller.UpdateRoom(room.Id, dto);

            Assert.IsType<NoContentResult>(result);
            var updated = await _context.Rooms.FindAsync(room.Id);
            Assert.Equal("Neu", updated.Name);
        }

        [Fact]
        public async Task UpdateRoom_ReturnsNotFound_WhenMissing()
        {
            var dto = new UpdateRoomDto { Name = "X", isAvailable = false };
            var result = await _controller.UpdateRoom(999, dto);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteRoom_Removes_WhenExists()
        {
            var room = new Room { Name = "DeleteMe", Capacity = 5, isAvailable = true };
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteRoom(room.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_context.Rooms);
        }

        [Fact]
        public async Task DeleteRoom_ReturnsNotFound_WhenMissing()
        {
            var result = await _controller.DeleteRoom(777);
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
