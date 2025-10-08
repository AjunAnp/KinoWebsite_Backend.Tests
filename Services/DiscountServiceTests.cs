using System;
using System.Threading.Tasks;
using KinoWebsite_Backend.Data;
using KinoWebsite_Backend.Models;
using KinoWebsite_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KinoWebsite_Backend.Tests.Services
{
    public class DiscountServiceTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddDiscount()
        {
            var db = GetDbContext();
            var service = new DiscountService(db);

            var discount = new Discount { Code = "SAVE10", Percentage = 10, ValidUntil = DateTime.UtcNow.AddDays(1), IsActive = true };

            var created = await service.CreateAsync(discount);

            Assert.NotNull(created);
            Assert.Single(db.Discounts);
        }

        [Fact]
        public async Task GetByCodeAsync_ShouldReturnDiscount()
        {
            var db = GetDbContext();
            var service = new DiscountService(db);

            var discount = new Discount { Code = "HELLO", Percentage = 15, ValidUntil = DateTime.UtcNow.AddDays(5), IsActive = true };
            db.Discounts.Add(discount);
            await db.SaveChangesAsync();

            var found = await service.GetByCodeAsync("HELLO");

            Assert.NotNull(found);
            Assert.Equal(15, found.Percentage);
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyDiscount_WhenExists()
        {
            var db = GetDbContext();
            var service = new DiscountService(db);

            var discount = new Discount { Code = "OLD", Percentage = 5, ValidUntil = DateTime.UtcNow.AddDays(2), IsActive = true };
            db.Discounts.Add(discount);
            await db.SaveChangesAsync();

            var updatedDiscount = new Discount { Code = "OLD", Percentage = 50, ValidUntil = DateTime.UtcNow.AddDays(10), IsActive = false };
            var result = await service.UpdateAsync("OLD", updatedDiscount);

            Assert.True(result);
            var refreshed = await service.GetByCodeAsync("OLD");
            Assert.Equal(50, refreshed.Percentage);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveDiscount()
        {
            var db = GetDbContext();
            var service = new DiscountService(db);

            db.Discounts.Add(new Discount { Code = "DEL", Percentage = 10, ValidUntil = DateTime.UtcNow.AddDays(1), IsActive = true });
            await db.SaveChangesAsync();

            var result = await service.DeleteAsync("DEL");

            Assert.True(result);
            Assert.Empty(db.Discounts);
        }
    }
}
