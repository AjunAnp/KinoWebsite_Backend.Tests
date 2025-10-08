using System;
using System.Threading.Tasks;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KinoWebsite_Backend.Tests.Services
{
    public class ReviewServiceTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // frische DB für jeden Test
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreateReviewAsync_ShouldAddReview()
        {
            var db = GetDbContext();
            var service = new ReviewService(db);

            var review = new Review
            {
                Comment = "Super Film!",
                StarRating = 5,
                MovieId = 1
            };

            var created = await service.CreateReviewAsync(review);

            Assert.NotNull(created);
            Assert.Single(db.Reviews);
            Assert.Equal("Super Film!", created.Comment);
            Assert.Equal(5, created.StarRating);
        }

        [Fact]
        public async Task GetReviewByIdAsync_ShouldReturnCorrectReview()
        {
            var db = GetDbContext();
            var service = new ReviewService(db);

            var review = new Review { Comment = "Test Review", StarRating = 3, MovieId = 1 };
            db.Reviews.Add(review);
            await db.SaveChangesAsync();

            var found = await service.GetReviewByIdAsync(review.Id);

            Assert.NotNull(found);
            Assert.Equal("Test Review", found.Comment);
            Assert.Equal(3, found.StarRating);
        }

        [Fact]
        public async Task GetAllReviewsAsync_ShouldReturnAll()
        {
            var db = GetDbContext();
            var service = new ReviewService(db);

            db.Reviews.Add(new Review { Comment = "A", StarRating = 2, MovieId = 1 });
            db.Reviews.Add(new Review { Comment = "B", StarRating = 4, MovieId = 1 });
            await db.SaveChangesAsync();

            var reviews = await service.GetAllReviewsAsync();

            Assert.Equal(2, reviews.Count);
        }

        [Fact]
        public async Task UpdateReviewAsync_ShouldChangeValues()
        {
            var db = GetDbContext();
            var service = new ReviewService(db);

            var review = new Review { Comment = "Old Comment", StarRating = 1, MovieId = 1 };
            db.Reviews.Add(review);
            await db.SaveChangesAsync();

            review.Comment = "Updated Comment";
            review.StarRating = 5;

            var result = await service.UpdateReviewAsync(review.Id, review);

            Assert.True(result);
            var refreshed = await service.GetReviewByIdAsync(review.Id);
            Assert.Equal("Updated Comment", refreshed.Comment);
            Assert.Equal(5, refreshed.StarRating);
        }

        [Fact]
        public async Task DeleteReviewAsync_ShouldRemoveReview()
        {
            var db = GetDbContext();
            var service = new ReviewService(db);

            var review = new Review { Comment = "Löschen", StarRating = 1, MovieId = 1 };
            db.Reviews.Add(review);
            await db.SaveChangesAsync();

            var result = await service.DeleteReviewAsync(review.Id);

            Assert.True(result);
            Assert.Empty(db.Reviews);
        }

        [Fact]
        public async Task DeleteReviewAsync_ShouldReturnFalse_WhenNotExists()
        {
            var db = GetDbContext();
            var service = new ReviewService(db);

            var result = await service.DeleteReviewAsync(999);

            Assert.False(result);
        }
    }
}
