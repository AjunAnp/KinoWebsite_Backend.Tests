using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinoWebsite_Backend.Controllers;
using KinoWebsite_Backend.Services;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.DTOs;
using KinoWebsite_Backend.Data;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace KinoWebsite_Backend.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly AppDbContext _context;
        private readonly UserService _service;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            var mockEmail = new Mock<IEmailService>();

            var configData = new Dictionary<string, string>
            {
                { "PasswordReset:SecretKey", "TEST_SECRET_KEY_123" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();

            _service = new UserService(_context, mockEmail.Object, config);
            _controller = new UsersController(_service);
        }

        private string HashPassword(string password)
        {
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = md5.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        [Fact]
        public async Task GetUsers_ReturnsOk_WithListOfUsers()
        {
            _context.Users.AddRange(
                new User { Firstname = "John", Lastname = "Doe", Email = "john@example.com", Password = "pw", PhoneNumber = "123" },
                new User { Firstname = "Jane", Lastname = "Smith", Email = "jane@example.com", Password = "pw", PhoneNumber = "456" }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetUsers();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var users = Assert.IsAssignableFrom<IEnumerable<UserDto>>(ok.Value);
            Assert.Equal(2, users.Count());
        }

        [Fact]
        public async Task GetUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            var result = await _controller.GetUser(999);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetUser_ReturnsOk_WhenUserExists()
        {
            var user = new User
            {
                Firstname = "Max",
                Lastname = "Mustermann",
                Email = "max@example.com",
                Password = "pw",
                PhoneNumber = "000"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _controller.GetUser(user.Id);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<UserDto>(ok.Value);
            Assert.Equal("Max", dto.Firstname);
        }

        [Fact]
        public async Task Register_CreatesNewUser_AndReturnsCreated()
        {
            var dto = new UserRegisterDto
            {
                Firstname = "Anna",
                Lastname = "Müller",
                Email = "anna@example.com",
                Password = "secret",
                PhoneNumber = "55555"
            };

            var result = await _controller.Register(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var userDto = Assert.IsType<UserDto>(created.Value);
            Assert.Equal("Anna", userDto.Firstname);
            Assert.Single(_context.Users);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenEmailExists()
        {
            _context.Users.Add(new User
            {
                Firstname = "Tom",
                Lastname = "Tester",
                Email = "tom@example.com",
                Password = "pw",
                PhoneNumber = "999"
            });
            await _context.SaveChangesAsync();

            var dto = new UserRegisterDto
            {
                Firstname = "Tom",
                Lastname = "Tester2",
                Email = "tom@example.com",
                Password = "pass",
                PhoneNumber = "111"
            };

            var result = await _controller.Register(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("E-Mail", bad.Value.ToString());
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenCredentialsCorrect()
        {
            var hashed = HashPassword("secret");
            _context.Users.Add(new User
            {
                Firstname = "Lena",
                Lastname = "Login",
                Email = "lena@example.com",
                Password = hashed,
                PhoneNumber = "123"
            });
            await _context.SaveChangesAsync();

            var dto = new UserLoginDto { Email = "lena@example.com", Password = "secret" };

            var result = await _controller.Login(dto);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var userDto = Assert.IsType<UserDto>(ok.Value);
            Assert.Equal("Lena", userDto.Firstname);
            Assert.NotNull(userDto.LastLogin);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenInvalidCredentials()
        {
            var dto = new UserLoginDto { Email = "wrong@example.com", Password = "nopass" };

            var result = await _controller.Login(dto);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Contains("Ungültige", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task UpdateUser_ChangesData_WhenExists()
        {
            var user = new User
            {
                Firstname = "Old",
                Lastname = "Name",
                Email = "old@example.com",
                Password = "pw",
                PhoneNumber = "000"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new UserUpdateDto
            {
                Firstname = "New",
                Lastname = "Name",
                PhoneNumber = "111",
                email = "new@example.com"
            };

            var result = await _controller.UpdateUser(user.Id, dto);

            Assert.IsType<NoContentResult>(result);
            var updated = await _context.Users.FindAsync(user.Id);
            Assert.Equal("New", updated.Firstname);
            Assert.Equal("new@example.com", updated.Email);
            Assert.Equal("111", updated.PhoneNumber);
        }

        [Fact]
        public async Task UpdateUser_ReturnsNotFound_WhenUserMissing()
        {
            var dto = new UserUpdateDto
            {
                Firstname = "X",
                Lastname = "Y",
                PhoneNumber = "999"
            };

            var result = await _controller.UpdateUser(99, dto);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteUser_RemovesUser_WhenExists()
        {
            var user = new User
            {
                Firstname = "Del",
                Lastname = "User",
                Email = "del@example.com",
                Password = "pw",
                PhoneNumber = "555"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteUser(user.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_context.Users);
        }

        [Fact]
        public async Task DeleteUser_ReturnsNotFound_WhenNotExists()
        {
            var result = await _controller.DeleteUser(999);
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
