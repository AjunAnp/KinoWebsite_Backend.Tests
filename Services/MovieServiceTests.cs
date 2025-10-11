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
    public class MovieServiceTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private Movie CreateMovie(string title = "Testfilm")
        {
            return new Movie
            {
                Title = title,
                Description = "Testbeschreibung",
                Duration = 120,
                Genre = "Action",
                Director = "Tester",
                ReleaseDate = DateTime.UtcNow,
                ImDbRating = 8.5,
                TrailerUrl = "http://example.com/trailer",
                ImageUrl = "http://example.com/img.jpg",
                AgeRestriction = AgeRestriction.UsK12, // Enum-Wert
                Cast = Array.Empty<string>(), // non-null array
                Shows = new List<Show>(),
                Reviews = new List<Review>()
            };
        }

        [Fact]
        public async Task CreateAsync_ShouldAddMovie()
        {
            var db = GetDbContext();
            var service = new MovieService(db);

            var movie = CreateMovie("Inception");

            var created = await service.CreateAsync(movie);

            Assert.NotNull(created);
            Assert.Equal(1, await db.Movies.CountAsync());
            Assert.Equal("Inception", (await db.Movies.FirstAsync()).Title);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMovie_WhenExists()
        {
            var db = GetDbContext();
            var movie = CreateMovie("Matrix");
            db.Movies.Add(movie);
            await db.SaveChangesAsync();

            var service = new MovieService(db);
            var found = await service.GetByIdAsync(movie.Id);

            Assert.NotNull(found);
            Assert.Equal("Matrix", found.Title);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            var db = GetDbContext();
            var service = new MovieService(db);

            var found = await service.GetByIdAsync(999);

            Assert.Null(found);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllMovies()
        {
            var db = GetDbContext();
            db.Movies.AddRange(CreateMovie("A"), CreateMovie("B"));
            await db.SaveChangesAsync();

            var service = new MovieService(db);
            var movies = await service.GetAllAsync();

            Assert.Equal(2, movies.Count);
            Assert.Contains(movies, m => m.Title == "A");
            Assert.Contains(movies, m => m.Title == "B");
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyMovie_WhenIdMatches()
        {
            var db = GetDbContext();
            var movie = CreateMovie("Old Title");
            db.Movies.Add(movie);
            await db.SaveChangesAsync();

            var service = new MovieService(db);

            movie.Title = "New Title";
            movie.Description = "Updated Description";
            movie.Duration = 150;

            var result = await service.UpdateAsync(movie.Id, movie);

            Assert.True(result);

            var refreshed = await db.Movies.FindAsync(movie.Id);
            Assert.Equal("New Title", refreshed.Title);
            Assert.Equal("Updated Description", refreshed.Description);
            Assert.Equal(150, refreshed.Duration);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnFalse_WhenIdDoesNotMatch()
        {
            var db = GetDbContext();
            var service = new MovieService(db);

            var movie = CreateMovie("Wrong ID");
            db.Movies.Add(movie);
            await db.SaveChangesAsync();

            var result = await service.UpdateAsync(movie.Id + 1, movie);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveMovie_WhenExists()
        {
            var db = GetDbContext();
            var movie = CreateMovie("Delete Me");
            db.Movies.Add(movie);
            await db.SaveChangesAsync();

            var service = new MovieService(db);
            var result = await service.DeleteAsync(movie.Id);

            Assert.True(result);
            Assert.Empty(db.Movies);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
        {
            var db = GetDbContext();
            var service = new MovieService(db);

            var result = await service.DeleteAsync(999);

            Assert.False(result);
        }
    }
}
