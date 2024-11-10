using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Group6WebProject.Data;
using Group6WebProject.Models;
using System.Security.Claims;
using System.Text;
using Group6WebProject.Services;
using Microsoft.EntityFrameworkCore;

namespace Group6WebProject.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public OrderController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: /Order/Checkout
        // GET: /Order/Checkout
        public async Task<IActionResult> Checkout()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Game)
                .FirstOrDefaultAsync(c => c.UserID == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            var creditCards = await _context.CreditCards
                .Where(cc => cc.UserID == userId)
                .ToListAsync();

            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .ToListAsync();

            ViewBag.CreditCards = creditCards;
            ViewBag.Addresses = addresses;

            return View(cart);
        }

        // POST: /Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int creditCardId, int addressId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Game)
                .FirstOrDefaultAsync(c => c.UserID == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            // Validate credit card and address
            var creditCard = await _context.CreditCards
                .FirstOrDefaultAsync(cc => cc.CreditCardID == creditCardId && cc.UserID == userId);
            if (creditCard == null)
            {
                ModelState.AddModelError("", "Invalid credit card selected.");
            }

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
            if (address == null)
            {
                ModelState.AddModelError("", "Invalid shipping address selected.");
            }

            if (!ModelState.IsValid)
            {
                // Reload credit cards and addresses
                var creditCards = await _context.CreditCards
                    .Where(cc => cc.UserID == userId)
                    .ToListAsync();

                var addresses = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .ToListAsync();

                ViewBag.CreditCards = creditCards;
                ViewBag.Addresses = addresses;

                return View(cart);
            }

            // Create Order
            var order = new Order
            {
                UserID = userId,
                OrderDate = DateTime.Now,
                CreditCardID = creditCardId,
                AddressID = addressId,
                Status = "Pending"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create OrderItems
            foreach (var cartItem in cart.CartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderID = order.OrderID,
                    GameID = cartItem.GameID,
                    Quantity = cartItem.Quantity,
                    Price = decimal.Parse(cartItem.Game.Price.Trim('$'))
                };
                _context.OrderItems.Add(orderItem);
            }

            // Clear Cart
            _context.CartItems.RemoveRange(cart.CartItems);

            await _context.SaveChangesAsync();

            // Retrieve the order with related data for the email
            var orderDetails = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Game)
                .Include(o => o.ShippingAddress)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderID == order.OrderID);

            // Build the email body with order details
            var emailBody = BuildOrderConfirmationEmail(orderDetails);

            // Send confirmation email
            await _emailService.SendEmailAsync(orderDetails.User.Email, "Order Confirmation", emailBody);

            // TempData["SuccessMessage"] = "Order placed successfully.";
            return RedirectToAction("OrderDetails", new { id = order.OrderID });
        }

        // GET: /Order/OrderDetails/5
        public async Task<IActionResult> OrderDetails(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Game)
                .Include(o => o.ShippingAddress)
                .Include(o => o.CreditCard)
                .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        private string BuildOrderConfirmationEmail(Order order)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<p>Dear {order.User.Name},</p>");
            sb.AppendLine($"<p>Thank you for your order <strong>#{order.OrderID}</strong> placed on {order.OrderDate.ToString("f")}.</p>");

            sb.AppendLine("<h3>Order Details:</h3>");
            sb.AppendLine("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Item</th>");
            sb.AppendLine("<th>Quantity</th>");
            sb.AppendLine("<th>Price</th>");
            sb.AppendLine("<th>Total</th>");
            sb.AppendLine("</tr>");

            decimal totalAmount = 0;

            foreach (var item in order.OrderItems)
            {
                var game = item.Game;
                decimal itemTotal = item.Quantity * item.Price;
                totalAmount += itemTotal;

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{game.Title}</td>");
                sb.AppendLine($"<td>{item.Quantity}</td>");
                sb.AppendLine($"<td>{item.Price:C}</td>");
                sb.AppendLine($"<td>{itemTotal:C}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");

            sb.AppendLine($"<p><strong>Total Amount:</strong> {totalAmount:C}</p>");

            sb.AppendLine("<h3>Shipping Address:</h3>");
            sb.AppendLine($"<p>{order.ShippingAddress.FullName}<br>");
            sb.AppendLine($"{order.ShippingAddress.StreetAddress}<br>");
            if (!string.IsNullOrEmpty(order.ShippingAddress.ApartmentSuite))
            {
                sb.AppendLine($"{order.ShippingAddress.ApartmentSuite}<br>");
            }

            sb.AppendLine($"{order.ShippingAddress.City}, {order.ShippingAddress.Province} {order.ShippingAddress.PostalCode}<br>");
            sb.AppendLine($"{order.ShippingAddress.Country}</p>");

            sb.AppendLine("<p>We will notify you once your order has been shipped.</p>");
            sb.AppendLine("<p>Thank you for shopping with us!</p>");
            sb.AppendLine("<p>Best regards,<br>Your Company Name</p>");

            return sb.ToString();
        }
    }
}