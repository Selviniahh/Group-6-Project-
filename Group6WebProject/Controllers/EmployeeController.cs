using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Group6WebProject.Data;
using Microsoft.EntityFrameworkCore;

namespace Group6WebProject.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsEmployee()
        {
            return User.IsInRole("Employee") || bool.TryParse(User.FindFirst("IsEmployee")?.Value, out bool isEmployee) && isEmployee;
        }

        // GET: /Employee/Orders
        public async Task<IActionResult> Orders()
        {
            if (!IsEmployee())
            {
                return Forbid();
            }

            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Game)
                .Include(o => o.ShippingAddress)
                .Where(o => o.Status == "Pending")
                .ToListAsync();

            return View(orders);
        }

        // POST: /Employee/ProcessOrder/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder(int orderId)
        {
            if (!IsEmployee())
            {
                return Forbid();
            }

            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = "Processed";
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Order #{order.OrderID} has been marked as processed.";

                return RedirectToAction("Orders", new { orderId = order.OrderID });
            }
            else
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction("Orders");
            }
        }

        // GET: /Employee/OrderDetails/5
        [Authorize]
        public async Task<IActionResult> OrderDetails(int orderId)
        {
            if (!IsEmployee())
            {
                return Forbid();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Game)
                .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}