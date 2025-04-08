using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebBiaProject.Data;
using WebBiaProject.Hubs;
using WebBiaProject.Models;

namespace WebBiaProject.Controllers
{
    [Authorize(Roles = "Customer")]
    public class BookingPageController : Controller
    {
        private readonly ILogger<BookingPageController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;

        public BookingPageController(
            ApplicationDbContext context,
            ILogger<BookingPageController> logger,
            UserManager<ApplicationUser> userManager,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        [HttpGet]
        public IActionResult Index(int x)
        {
            var userId = _userManager.GetUserId(User);

            var customer = _context.Customers
                .FirstOrDefault(c => c.UserId == userId);

            if (customer == null)
            {
                _logger.LogWarning("No customer found for user {UserId}", userId);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.CurrentCustomerId = customer.Id;

            var tables = _context.BilliardTables
                .Include(b => b.Branch)
                .Where(b => b.Id == x)
                .ToList();

            return View(tables);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
     
      
        public async Task<IActionResult> BookTable(int TableId, int BranchId, int CustomerId, DateTime StartTime, string PhoneNumber)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thông tin đặt bàn không hợp lệ.";
                _logger.LogWarning("Invalid booking details for TableId {TableId}", TableId);
                return RedirectToAction("Index");
            }

            if (StartTime < DateTime.Now)
            {
                TempData["Error"] = "Thời gian đặt phải từ hiện tại trở đi.";
                _logger.LogWarning("Invalid StartTime {StartTime} for TableId {TableId}", StartTime, TableId);
                return RedirectToAction("Index");
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var table = await _context.BilliardTables
                        .FirstOrDefaultAsync(t => t.Id == TableId);
                    if (table == null || table.StatusId != 1) // Giả sử 1 là "Sẵn Sàng"
                    {
                        TempData["Error"] = "Bàn không tồn tại hoặc không sẵn sàng.";
                        _logger.LogWarning("Table {TableId} not found or not available", TableId);
                        return RedirectToAction("Index");
                    }

                    if (await _context.Reservations.AnyAsync(r => r.TableId == TableId
                        && r.Status != "Cancelled"
                        && r.StartTime >= StartTime.AddMinutes(-30)
                        && r.StartTime <= StartTime.AddMinutes(30)))
                    {
                        TempData["Error"] = "Bàn đã được đặt trong khoảng thời gian này (±30 phút).";
                        _logger.LogWarning("Table {TableId} already booked at {StartTime}", TableId, StartTime);
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
                        CreatedBy = User.Identity?.Name ?? "Anonymous"
                    };

                    _context.Reservations.Add(reservation);
                    await _context.SaveChangesAsync();

                    // Gửi thông báo qua SignalR
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == CustomerId);
                    string customerName = customer?.Name ?? "Khách hàng không xác định";
                    await _hubContext.Clients.All.SendAsync("ReceiveReservationNotification",
                        $"Có đơn đặt hàng mới: DH{reservation.Id} từ {customerName} vào {StartTime:dd/MM/yyyy HH:mm}");

                    await transaction.CommitAsync();

                    _logger.LogInformation("Customer booked table {TableId} with reservation ID {ReservationId}", TableId, reservation.Id);
                    TempData["Success"] = "Đặt bàn thành công!";

                    return RedirectToAction("BookingSuccess", new { reservationId = reservation.Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error booking table {TableId}", TableId);
                    TempData["Error"] = "Đã xảy ra lỗi khi đặt bàn. Vui lòng thử lại.";
                    return RedirectToAction("Index");
                }
            }
        }

        [HttpGet]
        public IActionResult BookingSuccess(int reservationId)
        {
            ViewBag.ReservationId = reservationId;
            return View();
        }
    }
}