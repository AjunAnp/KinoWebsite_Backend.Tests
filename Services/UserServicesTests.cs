using System;
using System.Threading.Tasks;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.DTOs;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KinoWebsite_Backend.Tests.Services
{
    public class UserServiceTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Jede Testmethode frische DB
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task RegisterAsync_ShouldCreateUser_WhenDataIsValid()
        {
            var db = GetDbContext();
            var service = new UserService(db);

            var dto = new UserRegisterDto
            {
                Firstname = "Max",
                Lastname = "Mustermann",
                Email = "max@test.de",
                PhoneNumber = "123456",
                Password = "test123",
                IsAdmin = false
            };

            var user = await service.RegisterAsync(dto);

            Assert.NotNull(user);
            Assert.Equal("Max", user.Firstname);
            Assert.Single(db.Users);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnNull_WhenEmailAlreadyExists()
        {
            var db = GetDbContext();
            var service = new UserService(db);

            await service.RegisterAsync(new UserRegisterDto
            {
                Firstname = "Anna",
                Lastname = "Muster",
                Email = "anna@test.de",
                PhoneNumber = "123",
                Password = "pw",
                IsAdmin = false
            });

            var result = await service.RegisterAsync(new UserRegisterDto
            {
                Firstname = "Anna",
                Lastname = "Muster",
                Email = "anna@test.de",
                PhoneNumber = "123",
                Password = "pw",
                IsAdmin = false
            });

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnUser_WhenPasswordMatches()
        {
            var db = GetDbContext();
            var service = new UserService(db);

            await service.RegisterAsync(new UserRegisterDto
            {
                Firstname = "Lisa",
                Lastname = "Test",
                Email = "lisa@test.de",
                PhoneNumber = "999",
                Password = "secret",
                IsAdmin = false
            });

            var user = await service.LoginAsync("lisa@test.de", "secret");

            Assert.NotNull(user);
            Assert.Equal("lisa@test.de", user.Email);
            Assert.NotNull(user.LastLogin);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnNull_WhenWrongPassword()
        {
            var db = GetDbContext();
            var service = new UserService(db);

            await service.RegisterAsync(new UserRegisterDto
            {
                Firstname = "Tom",
                Lastname = "Test",
                Email = "tom@test.de",
                PhoneNumber = "888",
                Password = "pass123",
                IsAdmin = false
            });

            var user = await service.LoginAsync("tom@test.de", "falsch");

            Assert.Null(user);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllUsers()
        {
            var db = GetDbContext();
            var service = new UserService(db);

            await service.RegisterAsync(new UserRegisterDto
            {
                Firstname = "A",
                Lastname = "B",
                Email = "a@test.de",
                PhoneNumber = "1",
                Password = "pw",
                IsAdmin = false
            });

            await service.RegisterAsync(new UserRegisterDto
            {
                Firstname = "C",
                Lastname = "D",
                Email = "c@test.de",
                PhoneNumber = "2",
                Password = "pw",
                IsAdmin = false
            });

            var users = await service.GetAllAsync();

            Assert.Equal(2, users.Count);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectUser()
        {
            var db = GetDbContext();
            var service = new UserService(db);

            var user = await service.RegisterAsync(new UserRegisterDto
            {
                Firstname = "Eva",
                Lastname = "Test",
                Email = "eva@test.de",
                PhoneNumber = "777",
                Password = "pw",
                IsAdmin = false
            });

            var found = await service.GetByIdAsync(user.Id);

            Assert.NotNull(found);
            Assert.Equal("eva@test.de", found.Email);
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyUser_WhenExists()
        {
            var db = GetDbContext();
            var service = new UserService(db);

            var user = await service.RegisterAsync(new UserRegisterDto
            {
                Firstname = "Jan",
                Lastname = "Old",
                Email = "jan@test.de",
                PhoneNumber = "555",
                Password = "pw",
                IsAdmin = false
            });

            var dto = new UserUpdateDto
            {
                Firstname = "Jan",
                Lastname = "Neu",
                PhoneNumber = "9999"
            };

            var result = await service.UpdateAsync(user.Id, dto);

            Assert.True(result);

            var updated = await service.GetByIdAsync(user.Id);
            Assert.Equal("Neu", updated.Lastname);
            Assert.Equal("9999", updated.PhoneNumber);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveUser_WhenExists()
        {
            var db = GetDbContext();
            var service = new UserService(db);

            var user = await service.RegisterAsync(new UserRegisterDto
            {
                Firstname = "Karl",
                Lastname = "Löschen",
                Email = "karl@test.de",
                PhoneNumber = "111",
                Password = "pw",
                IsAdmin = false
            });

            var deleted = await service.DeleteAsync(user.Id);

            Assert.True(deleted);
            Assert.Empty(db.Users);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenUserDoesNotExist()
        {
            var db = GetDbContext();
            var service = new UserService(db);

            var result = await service.DeleteAsync(999);

            Assert.False(result);
        }
    }
}
