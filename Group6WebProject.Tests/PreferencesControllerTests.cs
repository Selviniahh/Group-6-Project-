using Group6WebProject.Controllers;
using Group6WebProject.Data;
using Group6WebProject.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Group6WebProject.Tests;

public class PreferencesControllerTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "PreferencesTestDb")
            .Options;
        return new ApplicationDbContext(options);
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
    public async Task PreferencesIndex_ReturnsPreferencesForExistingUser()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();
        var userId = 1;

        dbContext.MemberPreferences.Add(new MemberPreferences
        {
            UserId = userId,
            FavouritePlatforms = new List<string> { "PC" },
            FavouriteGameCategories = new List<string> { "Action" },
            LanguagePreferences = new List<string> { "English" }
        });
        await dbContext.SaveChangesAsync();

        var controller = new PreferencesController(dbContext);
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = GetMockUser(userId)
        };

        // Act
        var result = await controller.PreferencesIndex();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<MemberPreferences>(viewResult.Model);
        Assert.Equal(userId, model.UserId);
        Assert.Contains("PC", model.FavouritePlatforms);
    }

    [Fact]
    public async Task PreferencesIndex_CreatesDefaultPreferencesForNewUser()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();
        var userId = 2;

        var controller = new PreferencesController(dbContext);
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = GetMockUser(userId)
        };

        // Act
        var result = await controller.PreferencesIndex();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<MemberPreferences>(viewResult.Model);
        Assert.Equal(userId, model.UserId);
        Assert.Empty(model.FavouritePlatforms);
        Assert.Empty(model.FavouriteGameCategories);
    }

    [Fact]
    public async Task Edit_UpdatesExistingPreferences()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();
        var userId = 3;

        dbContext.MemberPreferences.Add(new MemberPreferences
        {
            UserId = userId,
            FavouritePlatforms = new List<string> { "Xbox" },
            FavouriteGameCategories = new List<string> { "Adventure" },
            LanguagePreferences = new List<string> { "German" }
        });
        await dbContext.SaveChangesAsync();

        var controller = new PreferencesController(dbContext);
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = GetMockUser(userId)
        };

        var updatedPreferences = new MemberPreferences
        {
            UserId = userId,
            FavouritePlatforms = new List<string> { "PlayStation" },
            FavouriteGameCategories = new List<string> { "Strategy" },
            LanguagePreferences = new List<string> { "French" }
        };

        // Act
        var result = await controller.Edit(updatedPreferences);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("PreferencesIndex", redirectResult.ActionName);

        var preferences = await dbContext.MemberPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        Assert.Contains("PlayStation", preferences.FavouritePlatforms);
        Assert.Contains("Strategy", preferences.FavouriteGameCategories);
        Assert.Contains("French", preferences.LanguagePreferences);
    }
}
