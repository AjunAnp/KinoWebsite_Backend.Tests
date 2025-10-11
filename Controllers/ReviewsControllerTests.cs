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
    public class ReviewsControllerTests
    {
        private readonly AppDbContext _context;
        private readonly ReviewService _service;
        private readonly ReviewsController _controller;

        public ReviewsControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // eigene DB pro Testlauf
                .Options;

            _context = new AppDbContext(options);
            _service = new ReviewService(_context);
            _controller = new ReviewsController(_service);
        }

        private Movie CreateTestMovie(string title = "Default Movie")
        {
            return new Movie
            {
                Title = title,
                Genre = "Drama",
                Duration = 120,
                Description = "Some description",
                Director = "John Director",
                ImageUrl = "http://example.com/image.jpg",
                TrailerUrl = "http://example.com/trailer.mp4",
                ReleaseDate = DateTime.UtcNow,
                ImDbRating = 8.0,
                Cast = Array.Empty<string>(),
                AgeRestriction = AgeRestriction.UsK12
            };
        }

        [Fact]
        public async Task GetReviews_ReturnsOk_WithList()
        {
            // Arrange
            var movie = CreateTestMovie("Film A");
            _context.Movies.Add(movie);
            _context.Reviews.Add(new Review
            {
                StarRating = 5,
                Comment = "Great!",
                MovieId = movie.Id
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetReviews();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<Review>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetReview_ReturnsNotFound_WhenMissing()
        {
            var result = await _controller.GetReview(99);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetReview_ReturnsOk_WhenExists()
        {
            // Arrange
            var movie = CreateTestMovie("Film X");
            _context.Movies.Add(movie);
            var review = new Review { StarRating = 4, Comment = "Nice!", Movie = movie };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetReview(review.Id);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var val = Assert.IsType<Review>(ok.Value);
            Assert.Equal("Nice!", val.Comment);
        }

        [Fact]
        public async Task CreateReview_ReturnsCreated_WhenValid()
        {
            // Arrange
            var movie = CreateTestMovie("Film B");
            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            var dto = new ReviewCreateDto
            {
                StarRating = 5,
                Comment = "Awesome",
                MovieId = movie.Id
            };

            // Act
            var result = await _controller.CreateReview(dto);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var val = Assert.IsType<Review>(created.Value);
            Assert.Equal(5, val.StarRating);
            Assert.Single(_context.Reviews);
        }

        [Fact]
        public async Task CreateReview_ReturnsBadRequest_WhenMovieMissing()
        {
            // Arrange
            var dto = new ReviewCreateDto
            {
                StarRating = 3,
                Comment = "Fail",
                MovieId = 999
            };

            // Act
            var result = await _controller.CreateReview(dto);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("nicht gefunden", bad.Value.ToString());
        }

        [Fact]
        public async Task UpdateReview_ChangesData_WhenExists()
        {
            // Arrange
            var movie = CreateTestMovie("Film C");
            _context.Movies.Add(movie);
            var review = new Review { StarRating = 2, Comment = "Old", Movie = movie };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var dto = new ReviewUpdateDto
            {
                StarRating = 4,
                Comment = "Updated"
            };

            // Act
            var result = await _controller.UpdateReview(review.Id, dto);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var updated = await _context.Reviews.FindAsync(review.Id);
            Assert.Equal("Updated", updated.Comment);
        }

        [Fact]
        public async Task UpdateReview_ReturnsNotFound_WhenMissing()
        {
            // Arrange
            var dto = new ReviewUpdateDto
            {
                StarRating = 1,
                Comment = "No"
            };

            // Act
            var result = await _controller.UpdateReview(999, dto);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("nicht gefunden", notFound.Value.ToString());
        }

        [Fact]
        public async Task DeleteReview_RemovesEntity_WhenExists()
        {
            // Arrange
            var movie = CreateTestMovie("Film D");
            _context.Movies.Add(movie);
            var review = new Review { StarRating = 5, Comment = "Bye", Movie = movie };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteReview(review.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_context.Reviews);
        }

        [Fact]
        public async Task DeleteReview_ReturnsNotFound_WhenMissing()
        {
            // Act
            var result = await _controller.DeleteReview(123);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
