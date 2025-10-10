using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinoWebsite_Backend.Controllers;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.DTOs;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using System;

namespace KinoWebsite_Backend.Tests.Controllers
{
    public class DiscountsControllerTests
    {
        private readonly AppDbContext _context;
        private readonly DiscountService _service;
        private readonly DiscountsController _controller;

        public DiscountsControllerTests()
        {
            // In-Memory-Datenbank für jeden Test mit eigener ID
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _service = new DiscountService(_context);
            _controller = new DiscountsController(_service);
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithDiscounts()
        {
            // Arrange
            _context.Discounts.AddRange(
                new Discount { Code = "SUMMER10", Percentage = 10, ValidUntil = DateTime.Now.AddDays(5), IsActive = true },
                new Discount { Code = "WINTER20", Percentage = 20, ValidUntil = DateTime.Now.AddDays(10), IsActive = false }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var discounts = Assert.IsAssignableFrom<IEnumerable<DiscountDto>>(okResult.Value);
            Assert.Collection(discounts,
                d => Assert.Equal("SUMMER10", d.Code),
                d => Assert.Equal("WINTER20", d.Code));
        }

        [Fact]
        public async Task GetByCode_ReturnsOk_WhenFound()
        {
            var discount = new Discount
            {
                Code = "TEST10",
                Percentage = 10,
                ValidUntil = DateTime.Now.AddDays(5),
                IsActive = true
            };
            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();

            var result = await _controller.GetByCode("TEST10");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<DiscountDto>(okResult.Value);
            Assert.Equal("TEST10", dto.Code);
        }

        [Fact]
        public async Task GetByCode_ReturnsNotFound_WhenNotExists()
        {
            var result = await _controller.GetByCode("INVALID");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Create_AddsDiscountAndReturnsCreated()
        {
            var dto = new DiscountDto
            {
                Code = "NEW15",
                Percentage = 15,
                ValidUntil = DateTime.Now.AddDays(7),
                IsActive = true
            };

            var result = await _controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedDto = Assert.IsType<DiscountDto>(created.Value);
            Assert.Equal("NEW15", returnedDto.Code);

            // Prüfen, ob in DB gespeichert
            var dbDiscount = await _context.Discounts.FirstOrDefaultAsync(d => d.Code == "NEW15");
            Assert.NotNull(dbDiscount);
        }

        [Fact]
        public async Task Update_ChangesExistingDiscount()
        {
            var discount = new Discount
            {
                Code = "UPDATE10",
                Percentage = 10,
                ValidUntil = DateTime.Now,
                IsActive = true
            };
            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();

            var dto = new DiscountDto
            {
                Code = "UPDATE10",
                Percentage = 50,
                ValidUntil = DateTime.Now.AddDays(2),
                IsActive = false
            };

            var result = await _controller.Update("UPDATE10", dto);

            Assert.IsType<NoContentResult>(result);

            var updated = await _context.Discounts.FirstOrDefaultAsync(d => d.Code == "UPDATE10");
            Assert.Equal(50, updated.Percentage);
            Assert.False(updated.IsActive);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenMissing()
        {
            var dto = new DiscountDto
            {
                Code = "MISSING",
                Percentage = 25,
                ValidUntil = DateTime.Now.AddDays(5),
                IsActive = true
            };

            var result = await _controller.Update("MISSING", dto);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_RemovesDiscount_WhenExists()
        {
            var discount = new Discount
            {
                Code = "DEL10",
                Percentage = 10,
                ValidUntil = DateTime.Now.AddDays(3),
                IsActive = true
            };
            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();

            var result = await _controller.Delete("DEL10");

            Assert.IsType<NoContentResult>(result);

            var dbCheck = await _context.Discounts.FirstOrDefaultAsync(d => d.Code == "DEL10");
            Assert.Null(dbCheck);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            var result = await _controller.Delete("UNKNOWN");
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
