using Group6WebProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Group6WebProject.Data;
using System.Security.Claims;
using System.Linq;
using System;

namespace Group6WebProject.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public AdminController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Helper method to check if the current user is an Admin
        private bool IsAdmin()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return false;
            }

            if (int.TryParse(userIdClaim, out int userId))
            {
                var user = _dbContext.Users.Find(userId);
                return user?.IsAdmin ?? false;
            }

            return false;
        }

        // Admin Dashboard (Only accessible by admin users)
        public IActionResult Index()
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            return View();
        }

        // Game Management Section
        public IActionResult GameManagement()
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            var games = _dbContext.Games.ToList();
            return View(games);
        }

        [HttpPost]
        public IActionResult AddGame(Game game)
        {
            if (ModelState.IsValid)
            {
                _dbContext.Games.Add(game);
                _dbContext.SaveChanges();
                TempData["SuccessMessage"] = "Game added successfully!";
                return RedirectToAction("GameManagement");
            }

            return View(game);
        }

        public IActionResult EditGame(int id)
        {
            var game = _dbContext.Games.Find(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        [HttpPost]
        public IActionResult EditGame(Game game)
        {
            if (ModelState.IsValid)
            {
                _dbContext.Games.Update(game);
                _dbContext.SaveChanges();
                TempData["SuccessMessage"] = "Game updated successfully!";
                return RedirectToAction("GameManagement");
            }

            return View(game);
        }

        public IActionResult DeleteGame(int id)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            var game = _dbContext.Games.Find(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        [HttpPost]
        public IActionResult DeleteGameConfirmed(int id)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            var game = _dbContext.Games.Find(id);
            if (game != null)
            {
                _dbContext.Games.Remove(game);
                _dbContext.SaveChanges();
                TempData["SuccessMessage"] = "Game deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Game not found!";
            }

            return RedirectToAction("GameManagement");
        }

        // Event Management Section
        public IActionResult EventManagement()
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            var events = _dbContext.Events.ToList();
            return View(events);
        }

        [HttpPost]
        public IActionResult AddEvent(Event eventItem)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                _dbContext.Events.Add(eventItem);
                _dbContext.SaveChanges();
                TempData["SuccessMessage"] = "Event added successfully!";
                return RedirectToAction("EventManagement");
            }

            return View(eventItem);
        }

        public IActionResult EditEvent(int id)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            var eventItem = _dbContext.Events.Find(id);
            if (eventItem == null)
            {
                return NotFound();
            }

            return View(eventItem);
        }

        [HttpPost]
        public IActionResult EditEvent(Event eventItem)
        {
            if (ModelState.IsValid)
            {
                _dbContext.Events.Update(eventItem);
                _dbContext.SaveChanges();
                TempData["SuccessMessage"] = "Event updated successfully!";
                return RedirectToAction("EventManagement");
            }

            return View(eventItem);
        }

        public IActionResult DeleteEvent(int id)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            var eventItem = _dbContext.Events.Find(id);
            if (eventItem == null)
            {
                return NotFound();
            }

            return View(eventItem);
        }

        [HttpPost]
        public IActionResult DeleteEventConfirmed(int id)
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            var eventItem = _dbContext.Events.Find(id);
            if (eventItem != null)
            {
                _dbContext.Events.Remove(eventItem);
                _dbContext.SaveChanges();
                TempData["SuccessMessage"] = "Event deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Event not found!";
            }

            return RedirectToAction("EventManagement");
        }

        public IActionResult ApproveReview(int reviewId)
        {
            var review = _dbContext.Reviews.Find(reviewId);
            if (review == null)
            {
                return NotFound();
            }

            return View("ReviewManagement");
        }

        [HttpPost]
        public IActionResult ApproveReviewConfirmed(int reviewId)
        {
            var review = _dbContext.Reviews.Find(reviewId);
            if (review == null)
            {
                return NotFound();
            }

            review.ReviewStatus = "Approved";

            _dbContext.Reviews.Update(review);
            _dbContext.SaveChanges();

            TempData["SuccessMessage"] = "Review has been approved!";
            return RedirectToAction("ReviewManagement");
        }

        public IActionResult RejectReview(int reviewId)
        {
            var review = _dbContext.Reviews.Find(reviewId);
            if (review == null)
            {
                return NotFound();
            }

            return View("ReviewManagement");
        }

        [HttpPost]
        public IActionResult RejectReviewConfirmed(int reviewId)
        {
            var review = _dbContext.Reviews.Find(reviewId);
            if (review == null)
            {
                return NotFound();
            }

            review.ReviewStatus = "Rejected";
          

            _dbContext.Reviews.Update(review);
            _dbContext.SaveChanges();

            TempData["ErrorMessage"] = "Review has been rejected!";
            return RedirectToAction("ReviewManagement");
        }

        public IActionResult ReviewManagement()
        {
            if (!IsAdmin())
            {
                return Forbid();
            }

            var pendingReviews = _dbContext.Reviews
                .Where(r => r.ReviewStatus == "Pending")
                .Include(r => r.Game)
                .Include(r => r.User)
                .ToList();

            return View(pendingReviews);
        }
    }
}
