using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Group6WebProject.Data;
using Group6WebProject.Models;
using Group6WebProject.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Group6WebProject.Controllers;

public class UserController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IReCaptchaService _reCaptchaService;

    public UserController(ApplicationDbContext dbContext, IEmailService emailService, IReCaptchaService reCaptchaService)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _reCaptchaService = reCaptchaService;
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ForgetPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<ViewResult> ForgetPassword(ForgetPasswordViewModel model)
    {
        // 1. Check if the given email exists, if not add error message to the model state
        var user = _dbContext.Users.FirstOrDefault(u => u.Email == model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Email does not exist.");
            return View(model);
        }

        //Generate a random password, hash and assign it to the user, update the DB 
        var newPassword = Guid.NewGuid().ToString().Substring(0, 20);
        var hashedPassword = HashPassword(newPassword);
        user.PasswordHash = hashedPassword;
        await _dbContext.SaveChangesAsync();

        //Send an email to the user with the new password
        var emailBody = $@"
            Hello {user.Name}, <br/><br/>
            Your password has been reset. Your new password is: <strong>{newPassword}</strong> <br/><br/>
            Please change your password after logging in from the Profiles tab. <br/><br/>";

        await _emailService.SendEmailAsync(user.Email, "Password Reset", emailBody);
        
        TempData["SuccessMessage"] = "Your new password has been emailed to your email account.";
        
        return View("Login");
    }


    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Retrieve the UserID from the claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //out simply means pass by ref like "int& userId" in C++ 
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return BadRequest("Invalid UserID format.");
        }

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return NotFound("User not found.");

        //Verify the current password 
        var currentPasswordHash = HashPassword(model.CurrentPassword);
        if (user.PasswordHash != currentPasswordHash)
        {
            ModelState.AddModelError(string.Empty, "Current password is incorrect");
            return View(model);
        }

        user.PasswordHash = HashPassword(model.NewPassword);
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your password has been changed successfully";
        return RedirectToAction("Profile");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }


    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Verify reCAPTCHA
            var reCaptchaResult = await _reCaptchaService.VerifyToken(model.ReCaptchaToken);
            if (!reCaptchaResult)
            {
                ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed. Please try again.");
                return View(model);
            }

            // Check if the email is already registered.
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                return View(model);
            }

            // Hash the password
            var passwordHash = HashPassword(model.Password);

            // Create the user entity
            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                PasswordHash = passwordHash,
                Status = EnrollmentStatus.ConfirmationMessageNotSent
            };

            // Add the user to the database
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Send confirmation email.
            var callbackUrl = Url.Action("ConfirmEmail", "User",
                new { userId = user.UserID }, HttpContext.Request.Scheme);

            var emailBody = $"Hello {user.Name},<br/><br/>" +
                            "Please confirm your email by clicking the link below:<br/><br/>" +
                            $"<a href=\"{callbackUrl}\">Confirm Email</a><br/><br/>" +
                            "Thank you and have a great day.";

            await _emailService.SendEmailAsync(user.Email, "Email Confirmation", emailBody);

            // Redirect to a page informing the user to check their email
            return RedirectToAction("RegistrationConfirmation");
        }

        return View(model);
    }


    [HttpGet]
    public IActionResult RegistrationConfirmation()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return NotFound("User not found.");

        if (user.Status == EnrollmentStatus.EnrollmentConfirmed) return View("EmailAlreadyConfirmed");

        user.Status = EnrollmentStatus.EnrollmentConfirmed;
        await _dbContext.SaveChangesAsync();

        return View("EmailConfirmed");
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var builder = new StringBuilder();
        foreach (var b in bytes) builder.Append(b.ToString("x2"));
        return builder.ToString();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Find the user by email
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            //If the user not found give error again
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt. Email does not exist");
                return View(model);
            }

            //If email not confirmed, give error
            if (user.Status != EnrollmentStatus.EnrollmentConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Email not confirmed.");
                return View(model);
            }

            //If user is locked out and lockout end time is in the future, give error 
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
            {
                ModelState.AddModelError(string.Empty, $"Account is locked. Try again at {user.LockoutEnd.Value}.");
                return View(model);
            }

            // Verify the password
            var passwordHash = HashPassword(model.Password);
            if (user.PasswordHash != passwordHash)
            {
                //If password is incorrect, increment failed login attempts once
                user.FailedLoginAttempts += 1;
                if (user.FailedLoginAttempts >= 3)
                {
                    //Lock out the log in attempt
                    user.LockOutCounter++;
                    user.LockoutEnd = DateTime.Now.AddMinutes(Data.User.LockOutTimer * user.LockOutCounter);
                    ModelState.AddModelError(string.Empty, "Account locked due to multiple failed login attempts. Try again later.");
                }
                else
                {
                    var attemptsLeft = 3 - user.FailedLoginAttempts;
                    ModelState.AddModelError(string.Empty, $"Invalid login attempt. You have {attemptsLeft} more attempt(s) before your account is locked.");
                }

                await _dbContext.SaveChangesAsync();
                return View(model);
            }

            //Since password is correct, reset failed login attempts
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _dbContext.SaveChangesAsync();

            // Create user claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email),
                new("IsAdmin", user.IsAdmin.ToString())
            };

            // Create claims identity
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Create authentication properties
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Make the cookie persistent
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
            };

            // Sign in the user with the claims principal
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Redirect to the home page
            return RedirectToAction("Index", "Home");
        }

        return View(model);
    }


    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        // Sign out from cookie authentication.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        // Retrieve the UserID from the claims as an int
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out int userId))
        {
            // UserID is not a valid integer
            return BadRequest("Invalid UserID format.");
        }
        // Check if the profile exists for the given UserID
        var existingProfile = await _dbContext.Profiles.FirstOrDefaultAsync(u => u.UserId == userId);

        if (existingProfile != null)
        {
            ViewBag.UserID = userId;
            return View(existingProfile);
        }

        // Create a new profile if one doesn't exist
        var model = new Profile
        {
            UserId = userId,
            Name = User.FindFirst(ClaimTypes.Name)?.Value,
            Gender = null,
            BirthDate = null,
            ReceiveCvgs = false
        };
        ViewBag.UserID = userId; 
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SaveProfile(Profile model)
    {
        if (ModelState.IsValid)
        {
            var existingProfile = _dbContext.Profiles.FirstOrDefault(p => p.UserId == model.UserId);
            if (existingProfile != null)
            {
                existingProfile.Gender = model.Gender;
                existingProfile.Name = model.Name;
                existingProfile.BirthDate = model.BirthDate;
                existingProfile.ReceiveCvgs = model.ReceiveCvgs;
            }
            else
            {
                // Create new profile
                _dbContext.Profiles.Add(model);
            }
        }

        // Save changes asynchronously
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your profile has been saved successfully.";
        return RedirectToAction("Profile"); // Redirect back to the "Profile" action after a successful save

        //Test
        
        //Friends and family list

    }

    [HttpGet]
    public async Task<IActionResult> FriendsAndFamilyDetails(int userId)
    {
        // Get the user and their friends and family list
        var user = await _dbContext.Users
            .Include(u => u.FriendsAndFamily)
            .FirstOrDefaultAsync(u => u.UserID == userId);
        
        // Get list recommended friends
        var potentialFriends = await _dbContext.Users
            .Where(u => u.UserID != userId && !user.FriendsAndFamily.Contains(u))
            .ToListAsync();

        // Show user and recommended friends to the view
        ViewBag.PotentialFriends = potentialFriends;

        return View(user);
    }

    // Add friend to friend list
    [HttpPost]
    public async Task<IActionResult> AddFriend(int userId, int friendId)
    {
        var user = await _dbContext.Users
            .Include(u => u.FriendsAndFamily)
            .FirstOrDefaultAsync(u => u.UserID == userId);

        var friend = await _dbContext.Users.FindAsync(friendId);

        if (user != null && friend != null && !user.FriendsAndFamily.Contains(friend))
        {
            user.FriendsAndFamily.Add(friend);
            await _dbContext.SaveChangesAsync();
        }
        //return to friends and family details page
        return RedirectToAction("FriendsAndFamilyDetails", new { userId });
    }
    
    

}