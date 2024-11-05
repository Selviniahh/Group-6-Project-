using Group6WebProject.Data;
using Group6WebProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Group6WebProject.Controllers
{
    public class GameController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GameController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Game/
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            if (string.IsNullOrEmpty(searchString))
            {
                ViewData["Message"] = "Please enter a search term";
            }

            // Find the games based on search criteria
            var games = _context.Games.Where(s =>
                s.Title.Contains(searchString) || 
                s.Description.Contains(searchString) || 
                s.Genre.Contains(searchString) || 
                s.Platform.Contains(searchString) || 
                s.ReleaseDate.ToString().Contains(searchString));

            var gamesList = await games.ToListAsync();

            if (!gamesList.Any())
            {
                ViewData["Message"] = "No games found matching your search results";
            }

            return View(gamesList);
        }

        // GET: /Game/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var game = await _context.Games
                .Include(g => g.Ratings)  // Include ratings
                .Include(g => g.Reviews)  // Include reviews
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null)
            {
                return NotFound();
            }

            // Calculate the average rating for this game
            double averageRating = 0;
            if (game.Ratings.Any())
            {
                averageRating = game.Ratings.Average(r => r.Rating);
            }

            // Find game recommendations based on the genre
            var gameRecommendations = await _context.Games
                .Where(g => g.Genre == game.Genre && g.Id != id)
                .ToListAsync();

            // Pass the data to the view
            ViewBag.GameRecommendations = gameRecommendations;
            ViewBag.AverageRating = averageRating;

            return View(game);
        }

        // POST: /Game/RateGame
        [HttpPost]
        public async Task<IActionResult> RateGame(int gameId, int rating)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(); // Ensure the user is authenticated
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Check if the rating already exists for this user
            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.UserID == userId && r.GameID == gameId);

            if (existingRating != null)
            {
                // Update the existing rating
                existingRating.Rating = rating;
                _context.Ratings.Update(existingRating);
            }
            else
            {
                // Add a new rating
                var newRating = new GameRating
                {
                    UserID = userId,
                    GameID = gameId,
                    Rating = rating
                };
                _context.Ratings.Add(newRating);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = gameId });
        }

        // POST: /Game/ReviewGame
        [HttpPost]
        public async Task<IActionResult> ReviewGame(int gameId, string reviewText)
        {
            if (string.IsNullOrWhiteSpace(reviewText))
            {
                TempData["ErrorMessage"] = "Review cannot be empty!";
                return RedirectToAction("Details", new { id = gameId });
            }

            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(); // Ensure the user is authenticated
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var newReview = new GameReview
            {
                UserID = userId,
                GameID = gameId,
                ReviewText = reviewText,
                ReviewStatus = "Pending" // Set review status to "Pending" for approval by an admin
            };

            _context.Reviews.Add(newReview);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your review has been submitted and is awaiting approval!";
            return RedirectToAction("Details", new { id = gameId });
        }
    }
}
