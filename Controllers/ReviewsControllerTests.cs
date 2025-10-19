using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinoWebsite_Backend.Controllers;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.DTOs;
using KinoWebsite_Backend.Services;

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
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _service = new ReviewService(_context);
            _controller = new ReviewsController(_service);
        }


        [Fact]
        public async Task GetReviews_ReturnsOk_WithList()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Matrix",
                Genre = "Action",
                Rating = 4.5,
                Cast = Array.Empty<string>(),
                Description = "Test",
                Director = "Test",
                ImageUrl = "none.jpg",
                TrailerUrl = "none.mp4"
            };

            _context.Movies.Add(movie);

            _context.Reviews.AddRange(
                new Review { StarRating = 5, Comment = "Super", Movie = movie, MovieId = movie.Id },
                new Review { StarRating = 3, Comment = "Okay", Movie = movie, MovieId = movie.Id }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetReviews();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var reviews = Assert.IsAssignableFrom<IEnumerable<ReviewDto>>(ok.Value);
            Assert.Equal(2, reviews.Count());
        }

        [Fact]
        public async Task GetReview_ReturnsNotFound_WhenMissing()
        {
            var result = await _controller.GetReview(999);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetReview_ReturnsOk_WhenExists()
        {
            var movie = new Movie
            {
                Title = "Matrix",
                Genre = "Action",
                Rating = 4.5,
                Cast = Array.Empty<string>(),
                Description = "Test",
                Director = "Test",
                ImageUrl = "none.jpg",
                TrailerUrl = "none.mp4"
            };

            _context.Movies.Add(movie);

            var review = new Review
            {
                StarRating = 4,
                Comment = "Great movie",
                Movie = movie,
                MovieId = movie.Id
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var result = await _controller.GetReview(review.Id);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<ReviewDto>(ok.Value);
            Assert.Equal("Great movie", dto.Comment);
            Assert.Equal(4, dto.StarRating);
        }

        [Fact]
        public async Task CreateReview_ReturnsCreated_WhenValid()
        {
            var movie = new Movie
            {
                Title = "Matrix",
                Genre = "Action",
                Rating = 4.5,
                Cast = Array.Empty<string>(),
                Description = "Test",
                Director = "Test",
                ImageUrl = "none.jpg",
                TrailerUrl = "none.mp4"
            };

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            var dto = new ReviewCreateDto
            {
                StarRating = 5,
                Comment = "Amazing!",
                MovieId = movie.Id
            };

            var result = await _controller.CreateReview(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var reviewDto = Assert.IsType<ReviewDto>(created.Value);
            Assert.Equal("Amazing!", reviewDto.Comment);
            Assert.Single(_context.Reviews);
        }

        [Fact]
        public async Task CreateReview_ReturnsBadRequest_WhenInvalidMovie()
        {
            var dto = new ReviewCreateDto
            {
                StarRating = 3,
                Comment = "No movie",
                MovieId = 999
            };

            var result = await _controller.CreateReview(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("message", bad.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateReview_ChangesData_WhenExists()
        {
            var movie = new Movie
            {
                Title = "Matrix",
                Genre = "Action",
                Rating = 4.5,
                Cast = Array.Empty<string>(),
                Description = "Test",
                Director = "Test",
                ImageUrl = "none.jpg",
                TrailerUrl = "none.mp4"
            };

            _context.Movies.Add(movie);

            var review = new Review
            {
                StarRating = 2,
                Comment = "Confusing",
                Movie = movie,
                MovieId = movie.Id
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var dto = new ReviewUpdateDto
            {
                StarRating = 4,
                Comment = "Actually pretty good"
            };

            var result = await _controller.UpdateReview(review.Id, dto);

            Assert.IsType<NoContentResult>(result);

            var updated = await _context.Reviews.FindAsync(review.Id);
            Assert.Equal("Actually pretty good", updated.Comment);
            Assert.Equal(4, updated.StarRating);
        }

        [Fact]
        public async Task UpdateReview_ReturnsNotFound_WhenMissing()
        {
            var dto = new ReviewUpdateDto
            {
                StarRating = 5,
                Comment = "Missing review"
            };

            var result = await _controller.UpdateReview(999, dto);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteReview_Removes_WhenExists()
        {
            var movie = new Movie
            {
                Title = "Matrix",
                Genre = "Action",
                Rating = 4.5,
                Cast = Array.Empty<string>(),
                Description = "Test",
                Director = "Test",
                ImageUrl = "none.jpg",
                TrailerUrl = "none.mp4"
            };

            _context.Movies.Add(movie);

            var review = new Review
            {
                StarRating = 2,
                Comment = "Delete me",
                Movie = movie,
                MovieId = movie.Id
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteReview(review.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_context.Reviews);
        }

        [Fact]
        public async Task DeleteReview_ReturnsNotFound_WhenMissing()
        {
            var result = await _controller.DeleteReview(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetReviewsByMovieId_ReturnsOnlyReviewsForMovie()
        {
            var movie1 = new Movie
            {
                Title = "Matrix",
                Genre = "Action",
                Rating = 4.5,
                Cast = Array.Empty<string>(),
                Description = "Test",
                Director = "Test",
                ImageUrl = "none.jpg",
                TrailerUrl = "none.mp4"
            };

            var movie2 = new Movie
            {
                Title = "Matrix",
                Genre = "Action",
                Rating = 4.5,
                Cast = Array.Empty<string>(),
                Description = "Test",
                Director = "Test",
                ImageUrl = "none.jpg",
                TrailerUrl = "none.mp4"
            };

            _context.Movies.AddRange(movie1, movie2);
            await _context.SaveChangesAsync();

            _context.Reviews.AddRange(
                new Review { StarRating = 5, Comment = "Top", Movie = movie1, MovieId = movie1.Id },
                new Review { StarRating = 3, Comment = "Ok", Movie = movie2, MovieId = movie2.Id }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetReviewsByMovieId(movie1.Id);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var reviews = Assert.IsAssignableFrom<IEnumerable<ReviewDto>>(ok.Value);
            Assert.Single(reviews);
            Assert.All(reviews, r => Assert.Equal(movie1.Id, r.MovieId));
        }
    }
}
