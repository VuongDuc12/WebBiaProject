using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiaProject.Data;
using WebBiaProject.Models;

namespace WebBiaProject.Controllers
{
    [Authorize(Roles = "Customer")]
    public class BookingPageController : Controller
    {
       

        private readonly ILogger<BookingPageController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; 
        public BookingPageController(ApplicationDbContext context, ILogger<BookingPageController> logger, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }
        [HttpGet]
        public  IActionResult Index(int x)
        {
            var userId = _userManager.GetUserId(User); // Returns the ASP.NET Identity UserId (string)

            // Assuming ApplicationUser has a CustomerId property linking to the Customer table
            var customer = _context.Customers
                .FirstOrDefault(c => c.UserId == userId); // Adjust based on your Customer model

            if (customer == null)
            {
                _logger.LogWarning("No customer found for user {UserId}", userId);
                return RedirectToAction("Index", "Home"); // Handle case where no customer is linked
            }

            // Set ViewBag.CurrentCustomerId
            ViewBag.CurrentCustomerId = customer.Id;

            // Fetch the billiard tables
            var tables = _context.BilliardTables
                .Include(b => b.Branch)
                .Where(b => b.Id == x)
                .ToList();

            return View(tables);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookTable(int TableId, int BranchId, int CustomerId,
        DateTime StartTime, string PhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid booking details.";
                return RedirectToAction("Index");
            }

            // Check availability
            if (_context.Reservations.Any(r => r.TableId == TableId && r.StartTime == StartTime && r.Status != "Cancelled"))
            {
                TempData["Error"] = "This table is already booked at the selected time.";
                return RedirectToAction("Index");
            }

            var reservation = new Reservation
            {
                TableId = TableId,
                BranchId = BranchId,
                CustomerId = CustomerId,
                StartTime = StartTime, 
                Status = "Pending",
                CreatedDate = DateTime.Now,
                CreatedBy = User.Identity?.Name,
              
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return RedirectToAction("BookingSuccess", new { reservationId = reservation.Id });
        }
        [HttpGet]
        public IActionResult BookingSuccess(int reservationId)
        {
            ViewBag.ReservationId = reservationId;
            return View();
        }
    }

}
