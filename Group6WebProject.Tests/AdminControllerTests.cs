using Group6WebProject.Controllers;
using Group6WebProject.Models;
using Group6WebProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Group6WebProject.Tests
{
    [TestFixture]
    public class AdminControllerTests
    {
        private AdminController _controller;
        
        private ApplicationDbContext _dbContext;
        private DbContextOptions<ApplicationDbContext> _options;
  

        // Mocks for the dependencies
        private Mock<ICompositeViewEngine> _mockViewEngine;
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private Mock<ITempDataProvider> _mockTempDataProvider;
        private Mock<IServiceProvider> _mockServiceProvider;

        [SetUp]
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

            // Create mocks for the dependencies
            _mockViewEngine = new Mock<ICompositeViewEngine>();
            _mockTempDataProvider = new Mock<ITempDataProvider>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();


            // Mock the ViewEngine to return a valid view
            _mockViewEngine.Setup(engine => engine.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(ViewEngineResult.Found("FakeView", new FakeView()));

            // Create the controller with the mocked dependencies
            _controller = new AdminController(
                _dbContext,
                _mockViewEngine.Object,
                _mockTempDataProvider.Object,
                _mockServiceProvider.Object,
                _mockHttpContextAccessor.Object
            );

            // Set up mock user
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

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        // FakeView class to simulate view rendering
        public class FakeView : IView
        {
            public string Path => "FakePath";

            public Task RenderAsync(ViewContext context)
            {
                return context.Writer.WriteAsync("<html><body>Fake View Content</body></html>");
            }
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

        //NEW TESTS FOR ITERATION 3
        //1
        [Test]
        public async Task GenerateGameListReport_ReturnsPdfFile()
        {
            _dbContext.Games.AddRange(new List<Game>
            {
                new Game { Id = 1, Title = "Game 1", Description = "Description 1", Genre = "Action", Price = "$10", Platform = "PC", ReleaseDate = DateTime.Now },
                new Game { Id = 2, Title = "Game 2", Description = "Description 2", Genre = "Adventure", Price = "$20", Platform = "Console", ReleaseDate = DateTime.Now }
            });
            _dbContext.SaveChanges();

            var result = await _controller.GenerateGameListReport("pdf");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<FileContentResult>());

            var fileResult = result as FileContentResult;
            Assert.That(fileResult.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(fileResult.FileDownloadName, Is.EqualTo("GameListReport.pdf"));
            Assert.That(fileResult.FileContents, Is.Not.Null);
            Assert.That(fileResult.FileContents.Length, Is.GreaterThan(0));
        }

        //2
        [Test]
        public async Task GenerateGameListReport_ReturnsExcelFile()
        {
            _dbContext.Games.AddRange(new List<Game>
            {
                new Game { Id = 1, Title = "Game 1", Description = "Description 1", Genre = "Action", Price = "$10", Platform = "PC", ReleaseDate = DateTime.Now },
                new Game { Id = 2, Title = "Game 2", Description = "Description 2", Genre = "Adventure", Price = "$20", Platform = "Console", ReleaseDate = DateTime.Now }
            });
            _dbContext.SaveChanges();

            var result = await _controller.GenerateGameListReport("excel");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<FileContentResult>());

            var fileResult = result as FileContentResult;
            Assert.That(fileResult.ContentType, Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
            Assert.That(fileResult.FileDownloadName, Is.EqualTo("GameListReport.xlsx"));
            Assert.That(fileResult.FileContents, Is.Not.Null);
            Assert.That(fileResult.FileContents.Length, Is.GreaterThan(0));
        }


        //3
        [Test]
        public async Task GenerateGameDetailReport_ReturnsPdfFile()
        {
            var user = _dbContext.Users.First(u => u.UserID == 2); // Get existing user

            var game = new Game
            {
                Id = 1,
                Title = "Game 1",
                Description = "Description 1",
                Genre = "Action",
                Price = "$10",
                Platform = "PC",
                ReleaseDate = DateTime.Now,
                Reviews = new List<GameReview>
                {
                    new GameReview
                    {
                        GameReviewID = 1,
                        UserID = user.UserID,
                        ReviewText = "Great game!",
                        SubmissionDate = DateTime.Now,
                        ReviewStatus = "Approved",
                        User = user // Use existing user
                    }
                }
            };
            _dbContext.Games.Add(game);
            _dbContext.SaveChanges();

            var result = await _controller.GenerateGameDetailReport(game.Id, "pdf");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<FileContentResult>());

            var fileResult = result as FileContentResult;
            Assert.That(fileResult.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(fileResult.FileDownloadName, Is.EqualTo($"GameDetailReport_{game.Title}.pdf"));
            Assert.That(fileResult.FileContents, Is.Not.Null);
            Assert.That(fileResult.FileContents.Length, Is.GreaterThan(0));
        }

        //4
        [Test]
        public async Task GenerateGameDetailReport_ReturnsExcelFile()
        {
            var user = _dbContext.Users.First(u => u.UserID == 2); // Get existing user

            var game = new Game
            {
                Id = 1,
                Title = "Game 1",
                Description = "Description 1",
                Genre = "Action",
                Price = "$10",
                Platform = "PC",
                ReleaseDate = DateTime.Now,
                Reviews = new List<GameReview>
                {
                    new GameReview
                    {
                        GameReviewID = 1,
                        UserID = user.UserID,
                        ReviewText = "Great game!",
                        SubmissionDate = DateTime.Now,
                        ReviewStatus = "Approved",
                        User = user // Use existing user
                    }
                }
            };
            _dbContext.Games.Add(game);
            _dbContext.SaveChanges();

            var result = await _controller.GenerateGameDetailReport(game.Id, "excel");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<FileContentResult>());

            var fileResult = result as FileContentResult;
            Assert.That(fileResult.ContentType, Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
            Assert.That(fileResult.FileDownloadName, Is.EqualTo($"GameDetailReport_{game.Title}.xlsx"));
            Assert.That(fileResult.FileContents, Is.Not.Null);
            Assert.That(fileResult.FileContents.Length, Is.GreaterThan(0));
        }
    }
}