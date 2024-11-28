using Group6WebProject.Data;
using Group6WebProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Group6WebProject.Controllers
{
    public class GameController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GameController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public IActionResult MyGames()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return RedirectToAction("Login", "User");
            }

            int userId = int.Parse(userIdClaim.Value);

            // Get purchased games
            var purchasedGames = _context.Orders
                .Where(o => o.UserID == userId)
                .SelectMany(o => o.OrderItems)
                .Select(oi => oi.Game)
                .Distinct()
                .ToList();

            var freeGames = _context.Games
                .AsEnumerable() // Switch to in-memory processing
                .Where(g =>
                {
                    // Remove the dollar sign if it exists
                    string priceStr = g.Price.StartsWith("$") ? g.Price.Substring(1) : g.Price;

                    // Attempt to parse the price to decimal
                    if (decimal.TryParse(priceStr, out decimal price))
                    {
                        return price == 0.00m;
                    }

                    // If parsing fails, exclude the game from the result
                    return false;
                })
                .ToList();

            // Combine the lists and remove duplicates
            var availableGames = purchasedGames.Union(freeGames).Distinct().ToList();

            return View(availableGames);
        }

        // GET: /Game/
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            if (string.IsNullOrWhiteSpace(searchString))
            {
                ModelState.AddModelError(string.Empty, "Please enter a search term.");
                return View(new List<Game>());
            }

            // Existing search logic
            var sanitizedSearchString = searchString.Trim();

            var games = _context.Games.Where(s =>
                EF.Functions.Like(s.Title, $"%{sanitizedSearchString}%") ||
                EF.Functions.Like(s.Description, $"%{sanitizedSearchString}%") ||
                EF.Functions.Like(s.Genre, $"%{sanitizedSearchString}%") ||
                EF.Functions.Like(s.Platform, $"%{sanitizedSearchString}%") ||
                EF.Functions.Like(s.ReleaseDate.ToString(), $"%{sanitizedSearchString}%")
            );

            var gamesList = await games.ToListAsync();

            if (!gamesList.Any())
            {
                ViewData["Message"] = "No games found matching your search results.";
            }

            return View(gamesList);
        }

        // GET: /Game/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var game = await _context.Games
                .Include(g => g.Ratings)
                .Include(g => g.Reviews)
                .ThenInclude(r => r.User)
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

            // Determine if the user has purchased the game
            bool hasPurchased = false;
            bool isFree = game.Price == "$0.00" || game.Price == "$0";

            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    var userId = int.Parse(userIdClaim);

                    hasPurchased = await _context.Orders
                        .Include(o => o.OrderItems)
                        .AnyAsync(o => o.UserID == userId && o.OrderItems.Any(oi => oi.GameID == id));
                }
            }

            // Create a view model to pass data to the view
            var viewModel = new GameDetailsViewModel
            {
                Game = game,
                AverageRating = averageRating,
                GameRecommendations = gameRecommendations,
                HasPurchased = hasPurchased,
                IsFree = isFree
            };

            return View(viewModel);
        }

        // POST: /Game/RateGame
        [HttpPost]
        public async Task<IActionResult> RateGame(int gameId, int rating)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim);

            // Check if the user has already rated this game
            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.UserID == userId && r.GameID == gameId);

            if (existingRating != null)
            {
                // Update the existing rating
                existingRating.Rating = rating;
                _context.Ratings.Update(existingRating);
                TempData["SuccessMessage"] = "Your rating has been updated!";
            }
            else
            {
                // Add the new rating if no existing rating
                var newRating = new GameRating
                {
                    UserID = userId,
                    GameID = gameId,
                    Rating = rating
                };
                _context.Ratings.Add(newRating);
                TempData["SuccessMessage"] = "Your rating has been submitted!";
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Now, calculate the average rating for the game
            var game = await _context.Games
                .Include(g => g.Ratings) // Include ratings to calculate average
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game != null)
            {
                // Calculate the new average rating after the new or updated rating has been added
                double averageRating = 0;
                if (game.Ratings.Any())
                {
                    averageRating = game.Ratings.Average(r => r.Rating);
                }

                ViewBag.AverageRating = averageRating;
            }

            // Redirect to the details page where the updated average rating will be shown
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

        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            // Check if the game is free or if the user has purchased it
            if (IsGameFree(game) || await HasUserPurchasedGame(id))
            {
                // Redirect to the download URL
                return Redirect(game.DownloadUrl);
            }
            else
            {
                TempData["ErrorMessage"] = "You do not have access to download this game.";
                return RedirectToAction("Details", new { id = id });
            }
        }

        private bool IsGameFree(Game game)
        {
            // Assuming that a price of "$0.00" or "$0" indicates a free game
            return game.Price == "$0.00" || game.Price == "$0";
        }

        private async Task<bool> HasUserPurchasedGame(int gameId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return false;
            }

            var userId = int.Parse(userIdClaim);

            // Check if the user has an order that includes this game
            var hasPurchased = await _context.Orders
                .Include(o => o.OrderItems)
                .AnyAsync(o => o.UserID == userId && o.OrderItems.Any(oi => oi.GameID == gameId));

            return hasPurchased;
        }

        public async Task<IActionResult> GameList()
        {
            var games = await _context.Games.ToListAsync();
            return View(games);
        }
    }
}