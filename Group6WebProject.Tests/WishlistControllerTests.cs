using Xunit;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Group6WebProject.Controllers;
using Group6WebProject.Data;
using Group6WebProject.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class WishListControllerTests
{
    [Fact]
    public async Task AddGameWishList_ShouldAddGame_WhenNotInWishlist()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "AddGameWishListDb")
            .Options;

        using var context = new ApplicationDbContext(options);

        // Seed data
        var game = new Game { Id = 1,
            Title = "Pathfinding Game",
            Description = "A Fun game to test and play pathfinding with adding obstacles",
            Genre = "Action",
            Price = "$19.99",
            Platform = "Windows",
            ReleaseDate = new DateTime(2023, 1, 1),
            DownloadUrl = "https://github.com/user-attachments/files/17656884/Pathfinding.zip",
            ImageFileName = "Pathfinding.jpg",
            VideoUrl = "https://github.com/user-attachments/assets/5ca9d61a-2f5b-4b58-a688-6ee62e15f2bc" };
        context.Games.Add(game);
        context.SaveChanges();

        var controller = new WishListController(context);

        // Mock User Identity
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
        }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Mock TempData
        controller.TempData = new TempDataDictionary(
            controller.ControllerContext.HttpContext,
            Mock.Of<ITempDataProvider>());

        // Act
        var result = await controller.AddGameWishList(1);

        // Assert
        var wishlistItems = await context.WishlistItems.ToListAsync();
        Assert.Single(wishlistItems);
        Assert.Equal(1, wishlistItems.First().UserId);
        Assert.Equal(1, wishlistItems.First().GameId);
        Assert.True(wishlistItems.First().IsPublic);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("WishListIndex", redirectResult.ActionName);
        Assert.Equal("Game added to wishlist.", controller.TempData["GameAddedMessage"]);
    }
    
    [Fact]
    public async Task RemoveFromWishList_ShouldRemoveGame_WhenInWishlist()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "RemoveFromWishListDb")
            .Options;

        using var context = new ApplicationDbContext(options);

        // Seed data
        var wishlistItem = new WishlistItem { UserId = 1, GameId = 1 };
        context.WishlistItems.Add(wishlistItem);
        context.SaveChanges();

        var controller = new WishListController(context);

        // Mock User Identity
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
        }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Mock TempData
        controller.TempData = new TempDataDictionary(
            controller.ControllerContext.HttpContext,
            Mock.Of<ITempDataProvider>());

        // Act
        var result = await controller.RemoveFromWishList(1);

        // Assert
        var wishlistItems = await context.WishlistItems.ToListAsync();
        Assert.Empty(wishlistItems);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("WishListIndex", redirectResult.ActionName);
        Assert.Equal("Game removed from wishlist.", controller.TempData["GameRemovedMessage"]);
    }
    
    [Fact]
    public async Task ViewFriendWishList_ShouldReturnWishlist_WhenFriendIsInFriendsAndFamily()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "ViewFriendWishListDb")
            .Options;

        using var context = new ApplicationDbContext(options);

        // Seed data
        var currentUser = new User { UserID = 1,
            Name = "Default User",
            Email = "DefaultUser@example.com",
            PasswordHash = UserController.HashPassword("123"),
            Status = EnrollmentStatus.EnrollmentConfirmed, };
        var friendUser = new User { UserID = 2,
            Name = "Default Admin",
            Email = "Admin@Admin.com",
            PasswordHash = UserController.HashPassword("Admin123"),
            Status = EnrollmentStatus.EnrollmentConfirmed,
            IsAdmin = true  };

        currentUser.FriendsAndFamily = new List<User> { friendUser };
        context.Users.Add(currentUser);
        context.Users.Add(friendUser);

        var game = new Game { Id = 1,
            Title = "Pathfinding Game",
            Description = "A Fun game to test and play pathfinding with adding obstacles",
            Genre = "Action",
            Price = "$19.99",
            Platform = "Windows",
            ReleaseDate = new DateTime(2023, 1, 1),
            DownloadUrl = "https://github.com/user-attachments/files/17656884/Pathfinding.zip",
            ImageFileName = "Pathfinding.jpg",
            VideoUrl = "https://github.com/user-attachments/assets/5ca9d61a-2f5b-4b58-a688-6ee62e15f2bc" };
        context.Games.Add(game);

        var wishlistItem = new WishlistItem
        {
            UserId = 2,
            GameId = 1,
            IsPublic = true,
            Game = game
        };
        context.WishlistItems.Add(wishlistItem);

        context.SaveChanges();

        var controller = new WishListController(context);

        // Mock User Identity
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
        }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Mock TempData
        controller.TempData = new TempDataDictionary(
            controller.ControllerContext.HttpContext,
            Mock.Of<ITempDataProvider>());

        // Act
        var result = await controller.ViewFriendWishList(2);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<WishlistItem>>(viewResult.Model);
        Assert.Single(model);
        Assert.Equal("Pathfinding Game", model.First().Game.Title);
        Assert.Equal(2, viewResult.ViewData["FriendUserId"]);
        Assert.Equal("Default Admin", viewResult.ViewData["FriendName"]);
        Assert.Equal(1, viewResult.ViewData["UserId"]);
    }
    
}
