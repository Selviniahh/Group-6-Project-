using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Group6WebProject.Controllers;
using Group6WebProject.Models;
using Group6WebProject.Data;
using Group6WebProject.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
namespace Group6WebProject.Tests
{
    public class UserControllerTests : IDisposable
    {
        private readonly UserController _controller;
        private readonly ApplicationDbContext _context;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<SignInManager<User>> _signInManagerMock;
        private readonly IReCaptchaService _reCaptchaService;

        public UserControllerTests()
        {
            // Set up in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Make Unique DB per test
                .Options;

            _context = new ApplicationDbContext(options); 

            // Mock services
            _emailServiceMock = new Mock<IEmailService>();

            // Mock UserManager
            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);

            // Mock SignInManager
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
            _signInManagerMock = new Mock<SignInManager<User>>(
                _userManagerMock.Object,
                contextAccessor.Object,
                userPrincipalFactory.Object,
                null, null, null, null
            );

            // Initialize the controller with the mocked services
            _controller = new UserController(_context, _emailServiceMock.Object, _reCaptchaService)
            {
                Url = GetMockUrlHelper("https://localhost/User/ConfirmEmail?userId=0"),
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Set HttpContext.Request.Scheme just like in the actual environment
            _controller.ControllerContext.HttpContext.Request.Scheme = "https";
        }

        // Create a mock IUrlHelper
        private static IUrlHelper GetMockUrlHelper(string returnValue)
        {
            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock.Setup(o => o.Action(It.IsAny<UrlActionContext>())).Returns(returnValue);
            return urlHelperMock.Object;
        }

        // Dispose method for cleaning up after each test
        public void Dispose()
        {
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
            var user = new User
            {
                UserID = 1,
                Name = "Test User",
                Email = "test@example.com",
                Status = EnrollmentStatus.EnrollmentConfirmed
            };

            // Add the user to the in-memory database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var model = new LoginViewModel
            {
                Email = user.Email,
                Password = "WrongPassword!"
            };

            // Mock UserManager's FindByEmailAsync method to return the user
            _userManagerMock
                .Setup(um => um.FindByEmailAsync(user.Email))
                .ReturnsAsync(user);

            // Mock SignInManager's PasswordSignInAsync method to simulate incorrect password
            _signInManagerMock
                .Setup(sm => sm.PasswordSignInAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Initialize HttpContext for the controller
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey(string.Empty)); // Check if ModelState has errors
            Assert.Equal("Invalid login attempt.", _controller.ModelState[string.Empty].Errors.First().ErrorMessage);
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
            Assert.Equal(userId, model.UserId);
            // Assert.Equal("Please describe briefly about yourself.", model.Biography);
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
                // Biography = "Initial Biography",
                // FavouriteVideoGame = "Initial Game",
                // DateOfBirth = new DateTime(1990, 1, 1),
                // Gender = Gender.Male,
                // Country = "Initial Country",
                // ContactNumber = "0000000000",
                // ReceivePromotionalEmails = false,
                // LastLogin = DateTime.Now
            };
            _context.Profiles.Add(initialProfile);
            await _context.SaveChangesAsync();

            // Modify the profile
            var updatedProfile = new Profile
            {
                UserId = user.UserID,
                Name = user.Name,
                // Biography = "Updated Biography",
                // FavouriteVideoGame = "Updated Game",
                // DateOfBirth = new DateTime(1990, 1, 1),
                // Gender = Gender.Female,
                // Country = "Updated Country",
                // ContactNumber = "1111111111",
                // ReceivePromotionalEmails = true
            };

            // **Act**
            var result = await _controller.SaveProfile(updatedProfile);

            // **Assert**
            // Assert.Equal("Updated Biography", savedProfile.Biography);
            // Assert.Equal("Updated Game", savedProfile.FavouriteVideoGame);
            // Assert.Equal(Gender.Female, savedProfile.Gender);
            // Assert.Equal("Updated Country", savedProfile.Country);
            // Assert.Equal("1111111111", savedProfile.ContactNumber);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirectResult.ActionName);
        }
    }
}