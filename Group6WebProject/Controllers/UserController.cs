using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Group6WebProject.Data;
using Group6WebProject.Models;
using Group6WebProject.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Group6WebProject.Controllers;

public class UserController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly UserManager<User> _userManager;  
    private readonly SignInManager<User> _signInManager;  

    public UserController(
        ApplicationDbContext dbContext, 
        IEmailService emailService, 
        UserManager<User> userManager,  
        SignInManager<User> signInManager)  
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _userManager = userManager;  
        _signInManager = signInManager;  
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
            // Check if the email is already registered. 
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                return View(model);
            }

            // Hash the password
            //var passwordHash = HashPassword(model.Password);

            // Create the user entity
            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                //PasswordHash = passwordHash,
                Status = EnrollmentStatus.ConfirmationMessageNotSent
            };

            // Add the user to the database
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Send confirmation email. 3rd argument is for ConfirmEmail method's argument value. 
            //protocol will be what being used in current Scheme which will be https or http
            //Example result: https://localhost:7015/User/ConfirmEmail?userId=4
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
        var sha256 = SHA256.Create();
        // Convert the password string to a byte array
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

        // Convert the byte array to a string
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

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        if (user.Status != EnrollmentStatus.EnrollmentConfirmed)
        {
            ModelState.AddModelError(string.Empty, "Email not confirmed.");
            return View(model);
        }

        // Sign in the user
        var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: true, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            // Fetch roles assigned to the user
            var roles = await _userManager.GetRolesAsync(user);

            // Attach roles as claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.Email, user.Email)
            };

            foreach (var role in roles)
            {
                //add roles
                claims.Add(new Claim(ClaimTypes.Role, role)); 
            }

            var claimsIdentity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
            };

            // Sign in with the new claims
            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToAction("Index", "Home");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }
    }

    return View(model);
}


    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        //Just simply sign out from cookie authentication.
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        //await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        // Retrieve the UserID from the claims as an int
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out int userId))
        {
            // Handle the case where UserID is not a valid integer
            return BadRequest("Invalid UserID format.");
        }

        // Check if the profile exists for the given UserID
        var existingProfile = await _dbContext.Profiles.FirstOrDefaultAsync(u => u.UserId == userId);
    
        if (existingProfile != null)
        {
            return View(existingProfile);
        }

        // Create a new profile if one doesn't exist
        var model = new Profile
        {
            UserId = userId,
            Name = User.FindFirst(ClaimTypes.Name)?.Value,
            Email = User.FindFirst(ClaimTypes.Email)?.Value,
            Biography = "Please describe briefly about yourself.",
            LastLogin = DateTime.Now,
            ReceivePromotionalEmails = false,
            Gender = Gender.NotSelected,
            Country = string.Empty,
            FavouriteVideoGame = string.Empty,
            ContactNumber = string.Empty,
            DateOfBirth = DateTime.Now
        };

        return View(model);
    }
    
private static readonly object _lock = new object();

public async Task<IActionResult> SaveProfile(Profile model)
{
    // Check if the country is valid (ensure it's not empty or null)
    if (string.IsNullOrWhiteSpace(model.Country))
    {
        TempData["ErrorMessage"] = "Country is required. Please select a valid country.";
        return View("Profile", model);  // Return to the "Profile" view with the model so the user can correct it
    }

    Profile existingProfile;

    lock (_lock)
    {
        existingProfile = _dbContext.Profiles.FirstOrDefault(p => p.Email == model.Email);
        if (existingProfile != null)
        {
            // Update existing profile
            existingProfile.ProfilePicture = model.ProfilePicture;
            existingProfile.FavouriteVideoGame = model.FavouriteVideoGame;
            existingProfile.Biography = model.Biography;
            existingProfile.DateOfBirth = model.DateOfBirth;
            existingProfile.Gender = model.Gender;
            existingProfile.Country = model.Country;
            existingProfile.ContactNumber = model.ContactNumber;
            existingProfile.ReceivePromotionalEmails = model.ReceivePromotionalEmails;
            existingProfile.LastLogin = DateTime.Now;
        }
        else
        {
            // Create new profile
            model.LastLogin = DateTime.Now;
            _dbContext.Profiles.Add(model);
        }
    }

    // Save changes asynchronously
    await _dbContext.SaveChangesAsync();

    TempData["SuccessMessage"] = "Your profile has been saved successfully.";
    return RedirectToAction("Profile");  // Redirect back to the "Profile" action after a successful save
}
}