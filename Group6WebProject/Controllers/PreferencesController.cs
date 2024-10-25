using System.Security.Claims;
using Group6WebProject.Data;
using Group6WebProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Group6WebProject.Controllers;

[Authorize] // Ensure only logged-in users can access
public class PreferencesController : Controller
{
    private readonly ApplicationDbContext _context;

    public PreferencesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Preferences
    public async Task<IActionResult> PreferencesIndex()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(userId, out int parsedUserId))
        {
            var preferences = await _context.MemberPreferences.FirstOrDefaultAsync(p => p.UserId == parsedUserId);

            if (preferences == null)
            { 
                preferences = new MemberPreferences
                {
                    UserId = parsedUserId,
                    FavouritePlatforms = new List<string>(),
                    FavouriteGameCategories = new List<string>(),
                    LanguagePreferences = new List<string>()
                };
                
                _context.MemberPreferences.Add(preferences);
                await _context.SaveChangesAsync();
                
                //return RedirectToAction("Create");
            }

            ViewBag.Platforms = GetPlatformOptions();
            ViewBag.Categories = GetCategoryOptions();
            ViewBag.Languages = GetLanguageOptions();
            return View(preferences);
        }

        return BadRequest("Invalid user ID.");
    }

    // GET: Preferences/Create
    public IActionResult Create()
    {
        ViewBag.Platforms = GetPlatformOptions();
        ViewBag.Categories = GetCategoryOptions();
        ViewBag.Languages = GetLanguageOptions();
        return View();
    }
    
    // POST: Preferences/Create
    [HttpPost]
    public async Task<IActionResult> Create(MemberPreferences preference)
    {
            
        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userId, out int parsedUserId))
            {
                preference.UserId = parsedUserId;
                _context.Add(preference);
                await _context.SaveChangesAsync();
                return RedirectToAction("PreferencesIndex");
            }
        }
        
        // If something goes wrong, reload the options
        ViewBag.Platforms = GetPlatformOptions();
        ViewBag.Categories = GetCategoryOptions();
        ViewBag.Languages = GetLanguageOptions();
        return View(preference);
    }
    
    // GET: Preferences/Edit
    public async Task<IActionResult> Edit()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(userId, out int parsedUserId))
        {
            var preferences = await _context.MemberPreferences
                .FirstOrDefaultAsync(p => p.UserId == parsedUserId);

            if (preferences == null)
            {
                return NotFound();
            }

            ViewBag.Platforms = GetPlatformOptions();
            ViewBag.Categories = GetCategoryOptions();
            ViewBag.Languages = GetLanguageOptions();
            return View(preferences);
        }

        return BadRequest("Invalid user ID.");
    }

    // POST: Preferences/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MemberPreferences preference)
    {
        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userId, out int parsedUserId))
            {
                var existingPreferences = await _context.MemberPreferences
                    .FirstOrDefaultAsync(p => p.UserId == parsedUserId);

                if (existingPreferences != null)
                {
                    existingPreferences.FavouritePlatforms = preference.FavouritePlatforms;
                    existingPreferences.FavouriteGameCategories = preference.FavouriteGameCategories;
                    existingPreferences.LanguagePreferences = preference.LanguagePreferences;

                    _context.Update(existingPreferences);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("PreferencesIndex");
                }
            }
        }
        
        // If something goes wrong, reload the options
        ViewBag.Platforms = GetPlatformOptions();
        ViewBag.Categories = GetCategoryOptions();
        ViewBag.Languages = GetLanguageOptions();
        return View(preference);
    }
    
    private static List<string> GetPlatformOptions()
    {
        return ["PC", "PlayStation", "Xbox", "Nintendo", "Mobile"];
    }

    private static List<string> GetCategoryOptions()
    {
        return ["Action", "Adventure", "Strategy", "Simulation", "Sports"];
    }

    private static List<string> GetLanguageOptions()
    {
        return ["English", "Spanish", "French", "German", "Japanese"];
    }

}