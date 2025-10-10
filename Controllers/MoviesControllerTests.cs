using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinoWebsite_Backend.Controllers;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.DTOs;
using KinoWebsite_Backend.Services;
using KinoWebsite_Backend.Data;
using System.Linq;

namespace KinoWebsite_Backend.Tests.Controllers
{
    public class MoviesControllerTests
    {
        private readonly AppDbContext _context;
        private readonly MovieService _service;
        private readonly MoviesController _controller;

        public MoviesControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // eigene DB pro Testlauf
                .Options;

            _context = new AppDbContext(options);
            _service = new MovieService(_context);
            _controller = new MoviesController(_service);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithMovies()
        {
            // Arrange
            _context.Movies.Add(new Movie
            {
                Title = "Inception",
                Genre = "Sci-Fi",
                Description = "Dream layers",
                Duration = 148,
                ReleaseDate = DateTime.UtcNow.AddYears(-10),
                TrailerUrl = "http://example.com/trailer",
                Director = "Christopher Nolan",
                ImDbRating = 8.8,
                Cast = new[] { "Leonardo DiCaprio" },
                ImageUrl = "http://example.com/image.jpg",
                AgeRestriction = AgeRestriction.UsK12
            });
            _context.SaveChanges();

            // Act
            var result = await _controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<MovieDto>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenMovieDoesNotExist()
        {
            // Act
            var result = await _controller.GetById(999);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenMovieExists()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Avatar",
                Genre = "Action",
                Description = "Blue aliens",
                Duration = 160,
                ReleaseDate = DateTime.UtcNow,
                TrailerUrl = "http://example.com/trailer",
                Director = "James Cameron",
                ImDbRating = 7.9,
                Cast = new[] { "Sam Worthington" },
                ImageUrl = "http://example.com/image.jpg",
                AgeRestriction = AgeRestriction.UsK12
            };
            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetById(movie.Id);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<MovieDto>(ok.Value);
            Assert.Equal("Avatar", dto.Title);
        }

        [Fact]
        public async Task Create_AddsMovie_AndReturnsCreatedAtAction()
        {
            // Arrange
            var dto = new MovieDto
            {
                Title = "Interstellar",
                Genre = "Sci-Fi",
                Description = "Space and time",
                Duration = 169,
                ReleaseDate = DateTime.UtcNow,
                TrailerUrl = "http://example.com/trailer",
                Director = "Christopher Nolan",
                ImDbRating = 8.6,
                Cast = new[] { "Matthew McConaughey" },
                ImageUrl = "http://example.com/image.jpg",
                AgeRestriction = "UsK12"
            };

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdDto = Assert.IsType<MovieDto>(created.Value);
            Assert.Equal("Interstellar", createdDto.Title);
            Assert.True(createdDto.Id > 0);
        }

        [Fact]
        public async Task Update_ChangesExistingMovie()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Old Title",
                Genre = "Drama",
                Description = "Old Desc",
                Duration = 100,
                ReleaseDate = DateTime.UtcNow,
                TrailerUrl = "t",
                Director = "Dir",
                ImDbRating = 7,
                Cast = new[] { "Someone" },
                ImageUrl = "img",
                AgeRestriction = AgeRestriction.UsK6
            };
            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            // 💡 wichtig: Tracking entfernen, damit EF keinen Konflikt bekommt
            _context.Entry(movie).State = EntityState.Detached;

            var dto = new MovieDto
            {
                Id = movie.Id,
                Title = "New Title",
                Genre = "Comedy",
                Description = "Updated",
                Duration = 120,
                ReleaseDate = movie.ReleaseDate,
                TrailerUrl = movie.TrailerUrl,
                Director = movie.Director,
                ImDbRating = movie.ImDbRating,
                Cast = movie.Cast,
                ImageUrl = movie.ImageUrl,
                AgeRestriction = "UsK6"
            };

            // Act
            var result = await _controller.Update(movie.Id, dto);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var updated = await _context.Movies.FindAsync(movie.Id);
            Assert.Equal("New Title", updated.Title);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenIdMismatch()
        {
            // Arrange
            var dto = new MovieDto
            {
                Id = 999,
                Title = "Wrong ID",
                Genre = "Drama",
                Description = "Mismatch",
                Duration = 90,
                ReleaseDate = DateTime.UtcNow,
                TrailerUrl = "t",
                Director = "Dir",
                ImDbRating = 6,
                Cast = new[] { "Actor" },
                ImageUrl = "img",
                AgeRestriction = "UsK6"
            };

            // Act
            var result = await _controller.Update(1, dto);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Delete_RemovesMovie_WhenExists()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Delete Me",
                Genre = "Horror",
                Description = "Test",
                Duration = 90,
                ReleaseDate = DateTime.UtcNow,
                TrailerUrl = "t",
                Director = "Dir",
                ImDbRating = 5.5,
                Cast = new[] { "Actor" },
                ImageUrl = "img",
                AgeRestriction = AgeRestriction.UsK18
            };
            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Delete(movie.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_context.Movies);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMovieDoesNotExist()
        {
            // Act
            var result = await _controller.Delete(12345);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
