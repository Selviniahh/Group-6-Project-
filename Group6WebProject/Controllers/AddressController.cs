using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Group6WebProject.Data;
using Group6WebProject.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Group6WebProject.Controllers
{
    [Authorize]
    public class AddressController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AddressController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Address
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .ToListAsync();
            return View(addresses);
        }

        // GET: Address/Create
        public IActionResult Create(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Address/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Address address, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                address.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                if (address.IsShippingSameAsMailing)
                {
                    address.ShippingStreetAddress = address.StreetAddress;
                    address.ShippingApartmentSuite = address.ApartmentSuite;
                    address.ShippingCity = address.City;
                    address.ShippingProvince = address.Province;
                    address.ShippingPostalCode = address.PostalCode;
                    address.ShippingCountry = address.Country;
                }

                _context.Add(address);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Index));
            }
            return View(address);
        }

        // GET: Address/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var address = await _context.Addresses.FindAsync(id);
            if (address == null || address.UserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)))
                return NotFound();

            return View(address);
        }

        // POST: Address/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Address address)
        {
            if (id != address.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    address.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                    // If shipping address is same as mailing, copy the mailing address fields to shipping
                    if (address.IsShippingSameAsMailing)
                    {
                        address.ShippingStreetAddress = address.StreetAddress;
                        address.ShippingApartmentSuite = address.ApartmentSuite;
                        address.ShippingCity = address.City;
                        address.ShippingProvince = address.Province;
                        address.ShippingPostalCode = address.PostalCode;
                        address.ShippingCountry = address.Country;
                    }

                    _context.Update(address);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AddressExists(address.Id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(address);
        }

        // GET: Address/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)));

            if (address == null)
                return NotFound();

            return View(address);
        }

        // POST: Address/Delete/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address != null && address.UserId == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)))
            {
                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AddressExists(int id)
        {
            return _context.Addresses.Any(e => e.Id == id && e.UserId == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)));
        }
    }
}