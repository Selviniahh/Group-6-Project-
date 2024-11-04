using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Group6WebProject.Data;
using Group6WebProject.Models;
using Microsoft.EntityFrameworkCore;

namespace Group6WebProject.Controllers;

[Authorize]
public class WishListController : Controller
{
    private readonly ApplicationDbContext _context;
    
    public WishListController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<IActionResult> WishListIndex()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var wishList = await _context.WishlistItems
            .Include(w => w.Game)
            .Where(w => w.UserId == userId)
            .ToListAsync();
        
            if (wishList == null || !wishList.Any())
            {
                TempData["NoGameMessage"] = "There is no game selected.";
                return RedirectToAction("Index","Game");
            }
        
        return View(wishList);
    }

    [HttpPost]
    public async Task<IActionResult> AddGameWishList(int gameId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // var existingWishListItem = await _context.WishlistItems
        //     .Where(w => w.UserId == userId && w.GameId == gameId)
        //     .FirstOrDefaultAsync();
        
        var existingWishListItem = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.GameId == gameId);

        if (existingWishListItem == null)
        {
            var wishListItem = new WishlistItem()
            {
                UserId = userId,
                GameId = gameId,
                IsPublic = true
            };

            _context.WishlistItems.Add(wishListItem);
             await _context.SaveChangesAsync(); 
             TempData["GameAddedMessage"] = "Game added to wishlist.";
        }
        else
        {
            TempData["ErrorMessage"] = "Game already in wishlist.";
        }
        
        return RedirectToAction("WishListIndex");
    }
    
    // [AllowAnonymous]
    // public async Task<IActionResult> GetWishList(int userId)
    // {
    //     var wishList = await _context.WishlistItems
    //         .Where(w => w.UserId == userId)
    //         .Include(w => w.Game)
    //         .ToListAsync();
    //
    //     return View(wishList);
    // }

    [HttpPost]
    public async Task<IActionResult> RemoveFromWishList(int? gameid)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        
        var existingWishListItem = await _context.WishlistItems
            .Where(w => w.UserId == userId && w.GameId == gameid)
            .FirstOrDefaultAsync();

        if (existingWishListItem != null)
        {
            _context.WishlistItems.Remove(existingWishListItem);
             await _context.SaveChangesAsync();
             TempData["GameRemovedMessage"] = "Game removed from wishlist.";
        }
        else
        {
            TempData["ErrorMessage"] = "Game not found in wishlist.";
            
        }
        
        return RedirectToAction("WishListIndex");
    }
}