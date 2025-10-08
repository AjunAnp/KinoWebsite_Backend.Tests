using System;
using System.Threading.Tasks;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KinoWebsite_Backend.Tests.Services
{
    public class SeatServiceTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreateSeatAsync_ShouldAddSeat_AndIncreaseCapacity()
        {
            var db = GetDbContext();
            var service = new SeatService(db);

            var room = new Room { Name = "Saal A", Capacity = 0 };
            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            var seat = new Seat { RoomId = room.Id, Row = 'A', SeatNumber = 1, Type = "Standard", isAvailable = true };
            var result = await service.CreateSeatAsync(seat);

            Assert.True(result.ok);
            Assert.NotNull(result.seat);
            var updatedRoom = await db.Rooms.FindAsync(room.Id);
            Assert.Equal(1, updatedRoom.Capacity);
        }

        [Fact]
        public async Task CreateSeatAsync_ShouldFail_WhenDuplicate()
        {
            var db = GetDbContext();
            var service = new SeatService(db);

            var room = new Room { Name = "Saal B", Capacity = 0 };
            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            db.Seats.Add(new Seat { RoomId = room.Id, Row = 'A', SeatNumber = 1 });
            await db.SaveChangesAsync();

            var seat = new Seat { RoomId = room.Id, Row = 'A', SeatNumber = 1, Type = "Standard" };
            var result = await service.CreateSeatAsync(seat);

            Assert.False(result.ok);
            Assert.Null(result.seat);
            Assert.Equal("Seat position already exists in this room.", result.error);
        }

        [Fact]
        public async Task UpdateSeatAsync_ShouldModifySeat()
        {
            var db = GetDbContext();
            var service = new SeatService(db);

            var room = new Room { Name = "Saal C" };
            db.Rooms.Add(room);
            var seat = new Seat { Room = room, Row = 'A', SeatNumber = 1, Type = "Standard" };
            db.Seats.Add(seat);
            await db.SaveChangesAsync();

            var result = await service.UpdateSeatAsync(seat.Id, 'B', 2, "VIP", false);

            Assert.True(result);
            var updated = await db.Seats.FindAsync(seat.Id);
            Assert.Equal('B', updated.Row);
            Assert.Equal(2, updated.SeatNumber);
            Assert.Equal("VIP", updated.Type);
            Assert.False(updated.isAvailable);
        }

        [Fact]
        public async Task SetAvailabilityForRowAsync_ShouldChangeAllSeats()
        {
            var db = GetDbContext();
            var service = new SeatService(db);

            var room = new Room { Name = "Saal D" };
            db.Rooms.Add(room);
            db.Seats.Add(new Seat { Room = room, Row = 'A', SeatNumber = 1, isAvailable = true });
            db.Seats.Add(new Seat { Room = room, Row = 'A', SeatNumber = 2, isAvailable = true });
            await db.SaveChangesAsync();

            var count = await service.SetAvailabilityForRowAsync(room.Id, 'A', false);

            Assert.Equal(2, count);
            var seats = await service.GetSeatsForRoomAsync(room.Id);
            Assert.All(seats, s => Assert.False(s.isAvailable));
        }

        [Fact]
        public async Task DeleteSeatAsync_ShouldRemoveSeat_AndDecreaseCapacity()
        {
            var db = GetDbContext();
            var service = new SeatService(db);

            var room = new Room { Name = "Saal E", Capacity = 1 };
            db.Rooms.Add(room);
            var seat = new Seat { Room = room, Row = 'A', SeatNumber = 1 };
            db.Seats.Add(seat);
            await db.SaveChangesAsync();

            var result = await service.DeleteSeatAsync(seat.Id);

            Assert.True(result);
            var updatedRoom = await db.Rooms.FindAsync(room.Id);
            Assert.Equal(0, updatedRoom.Capacity);
        }

        [Fact]
        public async Task DeleteAllSeatsInRoomAsync_ShouldClearSeats()
        {
            var db = GetDbContext();
            var service = new SeatService(db);

            var room = new Room { Name = "Saal F", Capacity = 2 };
            db.Rooms.Add(room);
            db.Seats.Add(new Seat { Room = room, Row = 'A', SeatNumber = 1 });
            db.Seats.Add(new Seat { Room = room, Row = 'A', SeatNumber = 2 });
            await db.SaveChangesAsync();

            var deletedCount = await service.DeleteAllSeatsInRoomAsync(room.Id);

            Assert.Equal(2, deletedCount);
            Assert.Empty(db.Seats);
            var updatedRoom = await db.Rooms.FindAsync(room.Id);
            Assert.Equal(0, updatedRoom.Capacity);
        }
    }
}
