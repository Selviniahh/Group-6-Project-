using Group6WebProject.Controllers;
using Group6WebProject.Models;
using Group6WebProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework; // Changed from Xunit to NUnit since your project uses NUnit
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Group6WebProject.Tests
{
    [TestFixture] // Add NUnit TestFixture attribute
    public class AdminControllerTests
    {
        private AdminController _controller;
        private ApplicationDbContext _dbContext;
        private DbContextOptions<ApplicationDbContext> _options;

        [SetUp] // Changed from constructor to SetUp method for NUnit
        public void Setup()
        {
            // Set up the in-memory database
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Create the DbContext
            _dbContext = new ApplicationDbContext(_options);

            // Seed the database with test admin user
            _dbContext.Users.Add(new User
            {
                UserID = 2,
                Name = "Admin User",
                Email = "admin@test.com",
                PasswordHash = "hashedpassword",
                IsAdmin = true
            });
            _dbContext.SaveChanges();

            // Create controller
            _controller = new AdminController(_dbContext);

            // Setup mock user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "2"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [TearDown] // Add cleanup
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Test] // Changed from Fact to Test for NUnit
        public void IsAdmin_ShouldReturnTrue_WhenUserIsAdmin()
        {
            // Act
            var result = _controller.IsAdmin();

            // Assert
            Assert.That(result, Is.True); // NUnit assertion style
        }

        [Test]
        public void AddGame_ShouldAddGame_WhenAdminIsAuthorized()
        {
            // Arrange
            var newGame = new Game
            {
                Id = 4,
                Title = "New Game",
                Description = "Description of the new game",
                Genre = "RPG",
                Price = "$49.99",
                Platform = "Xbox",
                ReleaseDate = new DateTime(2024, 1, 1),
                DownloadUrl = "https://github.com/user-attachments/files/17656884/Pathfinding.zip",
                ImageFileName = "Pathfinding.jpg",
                VideoUrl = "https://github.com/user-attachments/assets/30b28a29-06fd-4147-8b0e-5b79c9a4ef59"
            };

            // Act
            var result = _controller.AddGame(newGame);

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            
            var addedGame = _dbContext.Games.FirstOrDefault(g => g.Id == newGame.Id);
            Assert.That(addedGame, Is.Not.Null);
            Assert.That(addedGame.Title, Is.EqualTo(newGame.Title));
        }

        [Test]
        public void AddGame_ShouldReturnForbid_WhenUserIsNotAdmin()
        {
            // Arrange
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

            // Setup non-admin user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "3"), // Different user ID
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = _controller.AddGame(newGame);

            // Assert
            Assert.That(result, Is.TypeOf<ForbidResult>());
        }
    }
}