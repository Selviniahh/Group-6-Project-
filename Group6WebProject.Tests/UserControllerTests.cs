using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Group6WebProject.Controllers;
using Group6WebProject.Models;
using Group6WebProject.Data;
using Group6WebProject.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Group6WebProject.Tests
{
    public class UserControllerTests : IDisposable
    {
        private readonly UserController _controller;
        private readonly ApplicationDbContext _context;
        private readonly Mock<IEmailService> _emailServiceMock;

        //Everything inside this constructor will be used to initialize necessary objects to simulate a working environment for the tests
        //Mock type used to simulate the behavior of a real object. 
        public UserControllerTests()
        {
            // Set up in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Make Unique DB per test
                .Options; //This is necessary to convert DbContextOptionsBuilder to DbContextOptions 

            _context = new ApplicationDbContext(options); //This is the context that will be used in the tests. Just same as ApplicationDbContext in original project. 

            // Mock IEmailService. This is the only simple mocking requirement thankfully. 
            _emailServiceMock = new Mock<IEmailService>();

            // Mock IUrlHelper got unsuccessful because of the complexity of the method.

            // Initialize the controller. It will only make tests for the user that has id 0 
            _controller = new UserController(_context, _emailServiceMock.Object)
            {
                Url = GetMockUrlHelper("https://localhost/User/ConfirmEmail?userId=0"),
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Set HttpContext.Request.Scheme just like in program.cs
            _controller.ControllerContext.HttpContext.Request.Scheme = "https";
        }

        //It.Is or It.IsAny is used to check if the method is called with the correct parameters. 
        private static IUrlHelper GetMockUrlHelper(string returnValue)
        {
            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock.Setup(o => o.Action(It.IsAny<UrlActionContext>())).Returns(returnValue);
            return urlHelperMock.Object;
        }


        public void Dispose()
        {
            // Clean up the in-memory database after each test
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task ShouldRegisterUser()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            // Act
            await _controller.Register(model);

            // Assert
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            Assert.NotNull(user);
            Assert.Equal(model.Name, user.Name);
            Assert.Equal(model.Email, user.Email);
            Assert.Equal(EnrollmentStatus.ConfirmationMessageNotSent, user.Status);
        }

        [Fact]
        public async Task SendConfirmationEmail()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var result = await _controller.Register(model);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);


            // Check that the email service was called with correct parameters
            _emailServiceMock.Verify(es => es.SendEmailAsync(
                    model.Email,
                    "Email Confirmation",
                    It.Is<string>(s => s.Contains($"https://localhost/User/ConfirmEmail?userId={user.UserID}"))),
                Times.Once);

            // Check the result is a RedirectToAction
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("RegistrationConfirmation", redirectResult.ActionName);
        }

        [Fact]
        public async Task ExistingEmailShouldReturnError()
        {
            // Arrange
            var existingUser = new User
            {
                UserID = 1,
                Name = "Existing User",
                Email = "existing@example.com",
                PasswordHash = UserController.HashPassword("Password123!"),
                Status = EnrollmentStatus.EnrollmentConfirmed
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var model = new RegisterViewModel
            {
                Name = "New User",
                Email = "existing@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            // Act
            var result = await _controller.Register(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            var error = _controller.ModelState[string.Empty].Errors.First();
            Assert.Equal("An account with this email already exists.", error.ErrorMessage);
        }

        [Fact]
        public async Task IncorrectPasswordShouldReturnError()
        {
            // Arrange
            var password = "Password123!";
            var user = new User
            {
                UserID = 1,
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = UserController.HashPassword(password),
                Status = EnrollmentStatus.EnrollmentConfirmed
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var model = new LoginViewModel
            {
                Email = user.Email,
                Password = "WrongPassword!"
            };

            await Assert.ThrowsAsync<ArgumentNullException>(() => _controller.Login(model));
        }

        [Fact]
        public async Task UnConfirmedEmailShouldReturnError()
        {
            // Arrange
            var password = "Password123!";
            var user = new User
            {
                UserID = 1,
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = UserController.HashPassword(password),
                Status = EnrollmentStatus.ConfirmationMessageNotSent
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var model = new LoginViewModel
            {
                Email = user.Email,
                Password = password
            };

            // Act
            await _controller.Login(model);

            // Assert
            Assert.False(_controller.ModelState.IsValid);
            var error = _controller.ModelState[string.Empty].Errors.First();
            Assert.Equal("Email not confirmed.", error.ErrorMessage);
        }

        [Fact]
        public void HashAlgorithmWorksCorrect()
        {
            // Arrange
            string password = "password123";
            string expectedHash = "ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f";

            // Act
            string actualHash = UserController.HashPassword(password);

            // Assert
            Assert.Equal(expectedHash, actualHash);
        }


        [Fact]
        public async Task ProfileReturnsProfileModel()
        {
            // Arrange
            int userId = 1;
            var user = new User
            {
                UserID = userId,
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = UserController.HashPassword("Password123!"),
                Status = EnrollmentStatus.EnrollmentConfirmed
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            //Simulate httpcontext authentication 
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var result = await _controller.Profile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<Profile>(viewResult.Model);
        }

        [Fact]
        public async Task ProfileFetchedUserContentCorrectly()
        {
            // Arrange
            int userId = 1;
            var user = new User
            {
                UserID = userId,
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = UserController.HashPassword("Password123!"),
                Status = EnrollmentStatus.EnrollmentConfirmed
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            //Simulate httpcontext authentication 
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await _controller.Profile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Profile>(viewResult.Model);
            Assert.Equal(user.Name, model.Name);
            Assert.Equal(user.Email, model.Email);
            Assert.Equal(userId, model.UserId);
            Assert.Equal("Please describe briefly about yourself.", model.Biography);
        }

        [Fact]
        public async Task SaveProfileAppliesChanges()
        {
            // **Arrange**
            var user = new User
            {
                UserID = 2,
                Name = "Another User",
                Email = "another@example.com",
                PasswordHash = UserController.HashPassword("AnotherPassword!"),
                Status = EnrollmentStatus.EnrollmentConfirmed
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            // Simulate authenticated HttpContext
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Set up TempData again if ControllerContext is re-assigned
            var tempDataMock = new Mock<ITempDataProvider>();
            _controller.TempData = new TempDataDictionary(_controller.ControllerContext.HttpContext, tempDataMock.Object);

            // Create and save initial profile
            var initialProfile = new Profile
            {
                UserId = user.UserID,
                Name = user.Name,
                Email = user.Email,
                Biography = "Initial Biography",
                FavouriteVideoGame = "Initial Game",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = Gender.Male,
                Country = "Initial Country",
                ContactNumber = "0000000000",
                ReceivePromotionalEmails = false,
                LastLogin = DateTime.Now
            };
            _context.Profiles.Add(initialProfile);
            await _context.SaveChangesAsync();

            // Modify the profile
            var updatedProfile = new Profile
            {
                UserId = user.UserID,
                Name = user.Name,
                Email = user.Email,
                Biography = "Updated Biography",
                FavouriteVideoGame = "Updated Game",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = Gender.Female,
                Country = "Updated Country",
                ContactNumber = "1111111111",
                ReceivePromotionalEmails = true
            };

            // **Act**
            var result = await _controller.SaveProfile(updatedProfile);

            // **Assert**
            var savedProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.Email == user.Email);
            Assert.NotNull(savedProfile);
            Assert.Equal("Updated Biography", savedProfile.Biography);
            Assert.Equal("Updated Game", savedProfile.FavouriteVideoGame);
            Assert.Equal(Gender.Female, savedProfile.Gender);
            Assert.Equal("Updated Country", savedProfile.Country);
            Assert.Equal("1111111111", savedProfile.ContactNumber);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirectResult.ActionName);
        }
    }
}