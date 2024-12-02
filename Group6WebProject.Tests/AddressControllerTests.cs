using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Group6WebProject.Controllers;
using Group6WebProject.Data;
using Group6WebProject.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Group6WebProject.Tests;

public class AddressControllerTests
{
    private ApplicationDbContext GetInMemoryDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName) // Use a unique name for each test
            .EnableSensitiveDataLogging() // Enable detailed error logging
            .Options;
        var context = new ApplicationDbContext(options);
        context.Database.EnsureDeleted(); // Clear database to ensure test isolation
        return context;
    }

    private ClaimsPrincipal GetMockUser(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
    }

    [Fact]
    public async Task Index_ReturnsAddressesForUser()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext(nameof(Index_ReturnsAddressesForUser));
        var userId = 1;

        dbContext.Addresses.Add(new Address
        {
            Id = 1,
            UserId = userId,
            FullName = "John Doe",
            PhoneNumber = "123-456-7890",
            StreetAddress = "123 Test St",
            City = "Test City",
            Province = "Ontario",
            PostalCode = "A1B 2C3",
            Country = "Canada"
        });
        await dbContext.SaveChangesAsync();

        var controller = new AddressController(dbContext);
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = GetMockUser(userId)
        };

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Address>>(viewResult.Model);
        Assert.Single(model);
        Assert.Equal("123 Test St", model[0].StreetAddress);
    }

    [Fact]
    public async Task Create_PostValidAddress_AddsAddress()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext(nameof(Create_PostValidAddress_AddsAddress));
        var userId = 2;

        var controller = new AddressController(dbContext);
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = GetMockUser(userId)
        };

        var newAddress = new Address
        {
            FullName = "Jane Smith",
            PhoneNumber = "987-654-3210",
            StreetAddress = "456 New St",
            City = "New City",
            Province = "British Columbia",
            PostalCode = "X1Y 2Z3",
            Country = "Canada",
            IsShippingSameAsMailing = true
        };

        // Act
        var result = await controller.Create(newAddress);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AddressController.Index), redirectResult.ActionName);

        var addresses = dbContext.Addresses.Where(a => a.UserId == userId).ToList();
        Assert.Single(addresses);
        Assert.Equal("456 New St", addresses[0].StreetAddress);
        Assert.Equal("New City", addresses[0].ShippingCity);
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesAddress()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext(nameof(DeleteConfirmed_RemovesAddress));
        var userId = 4;

        dbContext.Addresses.Add(new Address
        {
            Id = 1,
            UserId = userId,
            FullName = "John Doe",
            PhoneNumber = "123-456-7890",
            StreetAddress = "Delete St",
            City = "Delete City",
            Province = "Ontario",
            PostalCode = "D1D 1D1",
            Country = "Canada"
        });
        await dbContext.SaveChangesAsync();

        var controller = new AddressController(dbContext);
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = GetMockUser(userId)
        };

        // Act
        var result = await controller.DeleteConfirmed(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AddressController.Index), redirectResult.ActionName);

        var address = await dbContext.Addresses.FirstOrDefaultAsync(a => a.Id == 1);
        Assert.Null(address);
    }
}
