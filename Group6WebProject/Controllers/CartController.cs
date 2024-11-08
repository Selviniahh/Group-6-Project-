using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Group6WebProject.Data;
using Group6WebProject.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Group6WebProject.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Cart/
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cart = await GetOrCreateCartAsync(userId);

            return View(cart);
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int gameId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cart = await GetOrCreateCartAsync(userId);

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartID == cart.CartID && ci.GameID == gameId);

            if (cartItem != null)
            {
                cartItem.Quantity += 1;
            }
            else
            {
                cartItem = new CartItem
                {
                    CartID = cart.CartID,
                    GameID = gameId,
                    Quantity = 1
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Game added to cart.";

            return RedirectToAction("Index", "Cart");
        }

        private async Task<Cart> GetOrCreateCartAsync(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Game)
                .FirstOrDefaultAsync(c => c.UserID == userId);

            if (cart == null)
            {
                cart = new Cart { UserID = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        // POST: /Cart/RemoveFromCart
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Item removed from cart.";
            }
            else
            {
                TempData["ErrorMessage"] = "Item not found in cart.";
            }

            return RedirectToAction("Index");
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem != null)
            {
                cartItem.Quantity = quantity;
                _context.CartItems.Update(cartItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cart updated.";
            }
            else
            {
                TempData["ErrorMessage"] = "Item not found in cart.";
            }

            return RedirectToAction("Index");
        }
    }
}
