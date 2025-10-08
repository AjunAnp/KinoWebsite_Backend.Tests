using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KinoWebsite_Backend.Tests.Services
{
    public class RoomServiceTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreateRoomAsync_ShouldAddRoom()
        {
            var db = GetDbContext();
            var service = new RoomService(db);

            var room = new Room { Name = "Saal 1", Capacity = 100, isAvailable = true };
            var created = await service.CreateRoomAsync(room);

            Assert.NotNull(created);
            Assert.Single(db.Rooms);
        }

        [Fact]
        public async Task GetRoomByIdAsync_ShouldReturnRoom()
        {
            var db = GetDbContext();
            var service = new RoomService(db);

            var room = new Room { Name = "Saal 2", Capacity = 50, isAvailable = true };
            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            var found = await service.GetRoomByIdAsync(room.Id);

            Assert.NotNull(found);
            Assert.Equal("Saal 2", found.Name);
        }

        [Fact]
        public async Task UpdateRoomAsync_ShouldChangeValues()
        {
            var db = GetDbContext();
            var service = new RoomService(db);

            var room = new Room { Name = "Alt", Capacity = 20, isAvailable = true };
            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            var update = new Room { Id = room.Id, Name = "Neu", Capacity = 30, isAvailable = false };
            var result = await service.UpdateRoomAsync(room.Id, update);

            Assert.True(result);
            var refreshed = await service.GetRoomByIdAsync(room.Id);
            Assert.Equal("Neu", refreshed.Name);
            Assert.Equal(30, refreshed.Capacity);
        }

        [Fact]
        public async Task DeleteRoomAsync_ShouldRemove_WhenNoSeats()
        {
            var db = GetDbContext();
            var service = new RoomService(db);

            var room = new Room { Name = "Löschen", Capacity = 10, isAvailable = true };
            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            var result = await service.DeleteRoomAsync(room.Id);

            Assert.True(result);
            Assert.Empty(db.Rooms);
        }

        [Fact]
        public async Task DeleteRoomAsync_ShouldFail_WhenSeatsExist()
        {
            var db = GetDbContext();
            var service = new RoomService(db);

            var room = new Room { Name = "Blockiert", Capacity = 10, isAvailable = true };
            db.Rooms.Add(room);
            db.Seats.Add(new Seat { Room = room, Row = 'A', SeatNumber = 1, isAvailable = true });
            await db.SaveChangesAsync();

            var result = await service.DeleteRoomAsync(room.Id);

            Assert.False(result);
        }

        [Fact]
        public async Task GenerateLayoutAsync_ShouldCreateSeats()
        {
            var db = GetDbContext();
            var service = new RoomService(db);

            var room = new Room { Name = "Layout", isAvailable = true };
            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            var seats = await service.GenerateLayoutAsync(room.Id, 2, 3);

            Assert.NotNull(seats);
            Assert.Equal(6, seats.Count); // 2 Reihen * 3 Sitze
        }

        [Fact]
        public async Task RecalculateCapacityAsync_ShouldUpdateCapacity()
        {
            var db = GetDbContext();
            var service = new RoomService(db);

            var room = new Room { Name = "Kapazität" };
            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            db.Seats.Add(new Seat { RoomId = room.Id, Row = 'A', SeatNumber = 1 });
            db.Seats.Add(new Seat { RoomId = room.Id, Row = 'A', SeatNumber = 2 });
            await db.SaveChangesAsync();

            var capacity = await service.RecalculateCapacityAsync(room.Id);

            Assert.Equal(2, capacity);
            var refreshed = await service.GetRoomByIdAsync(room.Id);
            Assert.Equal(2, refreshed.Capacity);
        }
    }
}
