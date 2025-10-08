using System;
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

        [Fact]
        public async Task CreateAsync_ShouldAddMovie()
        {
            var db = GetDbContext();
            var service = new MovieService(db);

            var movie = new Movie { Title = "Inception", Description = "Sci-Fi", Duration = 148 };
            var created = await service.CreateAsync(movie);

            Assert.NotNull(created);
            Assert.Single(db.Movies);
            Assert.Equal("Inception", created.Title);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMovie_WhenExists()
        {
            var db = GetDbContext();
            var service = new MovieService(db);

            var movie = new Movie { Title = "Matrix", Description = "Action", Duration = 136 };
            db.Movies.Add(movie);
            await db.SaveChangesAsync();

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
            var service = new MovieService(db);

            db.Movies.Add(new Movie { Title = "A" });
            db.Movies.Add(new Movie { Title = "B" });
            await db.SaveChangesAsync();

            var movies = await service.GetAllAsync();

            Assert.Equal(2, movies.Count);
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyMovie_WhenIdMatches()
        {
            var db = GetDbContext();
            var service = new MovieService(db);

            var movie = new Movie { Title = "Old Title", Duration = 100 };
            db.Movies.Add(movie);
            await db.SaveChangesAsync();

            movie.Title = "New Title";
            var result = await service.UpdateAsync(movie.Id, movie);

            Assert.True(result);
            var refreshed = await db.Movies.FindAsync(movie.Id);
            Assert.Equal("New Title", refreshed.Title);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnFalse_WhenIdDoesNotMatch()
        {
            var db = GetDbContext();
            var service = new MovieService(db);

            var movie = new Movie { Title = "Wrong" };
            db.Movies.Add(movie);
            await db.SaveChangesAsync();

            var otherMovie = new Movie { Id = 999, Title = "Other" };
            var result = await service.UpdateAsync(movie.Id, otherMovie);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveMovie_WhenExists()
        {
            var db = GetDbContext();
            var service = new MovieService(db);

            var movie = new Movie { Title = "Delete Me" };
            db.Movies.Add(movie);
            await db.SaveChangesAsync();

            var result = await service.DeleteAsync(movie.Id);

            Assert.True(result);
            Assert.Empty(db.Movies);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
        {
            var db = GetDbContext();
            var service = new MovieService(db);

            var result = await service.DeleteAsync(12345);

            Assert.False(result);
        }
    }
}
