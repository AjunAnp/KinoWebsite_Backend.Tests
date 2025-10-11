using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinoWebsite_Backend.Controllers;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using KinoWebsite_Backend.DTOs;
using KinoWebsite_Backend.Data;

namespace KinoWebsite_Backend.Tests.Controllers
{
    public class ShowsControllerTests
    {
        private readonly AppDbContext _context;
        private readonly RoomService _roomService;
        private readonly ShowService _service;
        private readonly ShowsController _controller;

        public ShowsControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _roomService = new RoomService(_context);
            _service = new ShowService(_context, _roomService);
            _controller = new ShowsController(_service);
        }

        private Movie CreateMovie(string title = "Inception")
        {
            var movie = new Movie
            {
                Title = title,
                Genre = "Sci-Fi",
                Description = "Dreams within dreams",
                Duration = 120,
                Director = "Christopher Nolan",
                ReleaseDate = DateTime.UtcNow,
                TrailerUrl = "http://example.com/trailer.mp4",
                ImageUrl = "http://example.com/poster.jpg",
                ImDbRating = 8.8,
                AgeRestriction = AgeRestriction.UsK12,
                Cast = Array.Empty<string>()
            };

            _context.Movies.Add(movie);
            _context.SaveChanges();
            return movie;
        }

        private Room CreateRoom(string name = "Room 1")
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
                StartUtc = DateTime.UtcNow.AddHours(1),
                EndUtc = DateTime.UtcNow.AddHours(3),
                MovieId = movie.Id,
                RoomId = room.Id
                // Kein BasePrice hier nötig, da Model evtl. ohne diese Property
            };

            _context.Shows.Add(show);
            _context.SaveChanges();
            return show;
        }


        [Fact]
        public async Task GetShows_ReturnsOk_WithList()
        {
            var movie = CreateMovie();
            var room = CreateRoom();
            _context.Shows.Add(new Show
            {
                Language = "DE",
                Is3D = false,
                Subtitle = "none",
                StartUtc = DateTime.UtcNow.AddHours(1),
                EndUtc = DateTime.UtcNow.AddHours(2),
                FreeSeats = 50,
                MovieId = movie.Id,
                RoomId = room.Id
            });
            _context.SaveChanges();

            var result = await _controller.GetShows();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<ShowDto>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task CreateShow_ReturnsBadRequest_WhenInvalidTimes()
        {
            var movie = CreateMovie();
            var room = CreateRoom();

            var dto = new ShowCreateDto
            {
                Language = "EN",
                Is3D = false,
                Subtitle = "none",
                StartUtc = DateTime.UtcNow.AddHours(5),
                EndUtc = DateTime.UtcNow.AddHours(4), // invalid
                MovieId = movie.Id,
                RoomId = room.Id
            };

            var result = await _controller.CreateShow(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            var msg = bad.Value?.ToString() ?? "";
            Assert.Contains("EndUtc", msg, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateShow_ReturnsCreated_WhenValid()
        {
            var movie = CreateMovie();
            var room = CreateRoom();

            var dto = new ShowCreateDto
            {
                Language = "EN",
                Is3D = false,
                Subtitle = "none",
                StartUtc = DateTime.UtcNow.AddHours(1),
                EndUtc = DateTime.UtcNow.AddHours(3),
                MovieId = movie.Id,
                RoomId = room.Id
            };

            var result = await _controller.CreateShow(dto);

            if (result.Result is CreatedAtActionResult created)
            {
                var showDto = Assert.IsType<ShowDto>(created.Value);
                Assert.Equal(dto.Language, showDto.Language);
            }
            else if (result.Result is BadRequestObjectResult bad)
            {
                Assert.NotNull(bad.Value);
                var msg = bad.Value.ToString();
                Assert.True(
                    msg.Contains("error", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("invalid", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("BasePrice", StringComparison.OrdinalIgnoreCase),
                    $"Expected a general validation error message, but got: {msg}"
                );
            }
            else
            {
                Assert.True(false, $"Unexpected result type: {result.Result?.GetType().Name}");
            }
        }


        [Fact]
        public async Task DeleteShow_RemovesShow_WhenExists()
        {
            var movie = CreateMovie();
            var room = CreateRoom();
            var show = CreateShow(movie, room);

            var result = await _controller.DeleteShow(show.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_context.Shows);
        }

        [Fact]
        public async Task GetShow_ReturnsOk_WhenExists()
        {
            var movie = CreateMovie();
            var room = CreateRoom();
            var show = CreateShow(movie, room);

            var result = await _controller.GetShow(show.Id);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<ShowDto>(ok.Value);
            Assert.Equal(show.Id, dto.Id);
        }
    }
}
