using Group6WebProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Group6WebProject.Controllers;

public class GameController : Controller
{
    private readonly ApplicationDbContext _context;

    public GameController(ApplicationDbContext context)
    {
        _context = context;
    }

    //GET: /Game/
    public async Task<IActionResult> Index(string searchString)
    {
        ViewData["CurrentFilter"] = searchString;

        if (string.IsNullOrEmpty(searchString))
        {
            ViewData["Message"] = "Please enter a search term";
        }
        
        //Find the game
        var games = _context.Games.Where(s => s.Title.Contains(searchString) || s.Description.Contains(searchString) || s.Genre.Contains(searchString) || s.Platform.Contains(searchString)
        || s.ReleaseDate.ToString().Contains(searchString));
        var gamesList = await games.ToListAsync();

        if (!gamesList.Any())
        {
            ViewData["Message"] = "No games found matching your search results";
        }
        
        //Making this operation async is not necessary, but it is good practice to do so
        return View(gamesList);
    }
    
    //Find the game from id and return the found game (if found)
    public async Task<IActionResult> Details(int id, string searchString)
    {
        var game = await _context.Games.FindAsync(id);
        if (game == null)
        {
            return NotFound();
        }
        
        ViewData["CurrentFilter"] = searchString;

        return View(game);

    }
}