using Group6WebProject.Controllers;
using Group6WebProject.Models;
using Group6WebProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Group6WebProject.Tests
{
    public class AdminControllerTests
    {
        private readonly AdminController _controller;
        private readonly ApplicationDbContext _dbContext;

        // Constructor to initialize the test context
        public AdminControllerTests()
        {
            // Set up the in-memory database for testing (with a unique database name for each test)
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())  // Use unique DB name for each test run
                .Options;

            // Create the DbContext with the in-memory database
            _dbContext = new ApplicationDbContext(options);

            // Seed the database with a test admin user
            _dbContext.Users.Add(new User
            {
                UserID = 2,
                Name = "Admin User",
                Email = "admin@test.com",
                PasswordHash = "hashedpassword", // Simulated password
                IsAdmin = true
            });
            _dbContext.SaveChanges();

            // Create the controller with the in-memory DbContext
            _controller = new AdminController(_dbContext);

            // Set up a mock ClaimsPrincipal to simulate a logged-in user
            var mockUser = new Mock<ClaimsPrincipal>();
            mockUser.Setup(u => u.FindFirst(ClaimTypes.NameIdentifier)).Returns(new Claim(ClaimTypes.NameIdentifier, "2"));

            // Assign the mock user to the controller's HttpContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = mockUser.Object }
            };
        }

        // Test 1: IsAdmin method should return true for an admin user
        [Fact]
        public void IsAdmin_ShouldReturnTrue_WhenUserIsAdmin()
        {
            // Act: Call the IsAdmin method in the controller
            var result = _controller.IsAdmin();

            // Assert: Verify that the result is true because the user is an admin
            Assert.True(result);
        }

        // Test 2: AddGame method should add a game when admin is authorized
        [Fact]
        public void AddGame_ShouldAddGame_WhenAdminIsAuthorized()
        {
            // Arrange: Create a new game to be added
            var newGame = new Game
            {
                Id = 4,
                Title = "New Game",
                Description = "Description of the new game",
                Genre = "RPG",
                Price = "$49.99",
                Platform = "Xbox",
                ReleaseDate = new DateTime(2024, 1, 1)
            };

            // Act: Add the new game using the controller method
            var result = _controller.AddGame(newGame);

            // Assert: Ensure the game was added by querying the in-memory database
            var addedGame = _dbContext.Games.FirstOrDefault(g => g.Id == newGame.Id); // Retrieve the game by ID
            Assert.NotNull(addedGame);  // The game should not be null if it was added successfully
            Assert.Equal(newGame.Title, addedGame.Title);  // Ensure the title matches

            // Assert: Should redirect to "GameManagement" page
            Assert.IsType<RedirectToActionResult>(result);
        }
        // Test 3: AddGame method should return Forbid when the user is not an admin
        [Fact]
        public void AddGame_ShouldReturnForbid_WhenUserIsNotAdmin()
        {
            // Arrange: Create a new game to be added
            var newGame = new Game
            {
                Id = 4,
                Title = "New Game",
                Description = "Description of the new game",
                Genre = "RPG",
                Price = "$49.99",
                Platform = "Xbox",
                ReleaseDate = new DateTime(2024, 1, 1)
            };

            // Mocking the IsAdmin method to return false (simulate a non-admin user)
            var mockUser = new Mock<ClaimsPrincipal>();
            mockUser.Setup(u => u.IsInRole("Admin")).Returns(false);  // Simulate a non-admin user
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext { User = mockUser.Object }
            };

            // Act: Try to add the new game
            var result = _controller.AddGame(newGame);

            // Assert: Should return a ForbidResult when the user is not an admin
            Assert.IsType<ForbidResult>(result);
        }
    }
}
