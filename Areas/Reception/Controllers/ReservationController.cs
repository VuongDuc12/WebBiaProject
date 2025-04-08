using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebBiaProject.Data;
using WebBiaProject.Models;

namespace WebBiaProject.Areas.Reception.Controllers
{
    [Area("Reception")]
    [Authorize(Roles = "Reception")]
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ReservationController> _logger;

        public ReservationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ReservationController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        private async Task<int?> GetManagerBranchId()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Current user not found. User claims: {@Claims}", User.Claims.Select(c => new { c.Type, c.Value }));
                    return null;
                }

                _logger.LogInformation("Current user found: {UserId}", user.Id);

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (employee == null)
                {
                    _logger.LogWarning("Employee record not found for user {UserId}.", user.Id);
                    return null;
                }

                _logger.LogInformation("Employee found with BranchId: {BranchId}", employee.BranchId);
                return employee.BranchId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting Manager BranchId.");
                return null;
            }
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Failed to determine branch for current user.");
                TempData["Error"] = "Không thể xác định chi nhánh của bạn.";
                return RedirectToAction("Index", "Home");
            }

            var query = _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Include(r => r.Branch)
                .Where(r => r.BranchId == branchId);

            if (startDate.HasValue)
            {
                query = query.Where(r => r.StartTime >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(r => r.StartTime <= endDate.Value.AddDays(1).AddTicks(-1));
            }

            var reservations = await query
                .OrderBy(r => r.StartTime)
                .ToListAsync();

            ViewBag.ReservationCount = reservations.Count;
            ViewBag.PendingCount = reservations.Count(r => r.Status == "Pending");
            ViewBag.ConfirmedCount = reservations.Count(r => r.Status == "Confirmed");
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(reservations);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Failed to determine branch for current user in Create action.");
                TempData["Error"] = "Không thể xác định chi nhánh của bạn.";
                return RedirectToAction("Index");
            }

            var viewModel = new ReservationViewModel
            {
                // Chỉ hiển thị các bàn "Sẵn Sàng" (StatusId = 1)
                AvailableTables = await _context.BilliardTables
                    .Where(t => t.BranchId == branchId && t.StatusId == 1)
                    .OrderBy(t => t.TableNumber)
                    .ToListAsync(),
                StartTime = DateTime.Now
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReservationViewModel model)
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Failed to determine branch for current user in Create POST action.");
                TempData["Error"] = "Không thể xác định chi nhánh của bạn.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                model.AvailableTables = await _context.BilliardTables
                    .Where(t => t.BranchId == branchId && t.StatusId == 1)
                    .OrderBy(t => t.TableNumber)
                    .ToListAsync();
                return View(model);
            }

            if (model.StartTime < DateTime.Now)
            {
                ModelState.AddModelError("StartTime", "Thời gian đặt phải từ hiện tại trở đi.");
                model.AvailableTables = await _context.BilliardTables
                    .Where(t => t.BranchId == branchId && t.StatusId == 1)
                    .OrderBy(t => t.TableNumber)
                    .ToListAsync();
                return View(model);
            }

            var table = await _context.BilliardTables
                .FirstOrDefaultAsync(t => t.Id == model.TableId && t.BranchId == branchId);
            if (table == null || table.StatusId != 1) // Kiểm tra bàn có sẵn sàng không
            {
                ModelState.AddModelError("TableId", "Bàn không tồn tại hoặc không sẵn sàng.");
                model.AvailableTables = await _context.BilliardTables
                    .Where(t => t.BranchId == branchId && t.StatusId == 1)
                    .OrderBy(t => t.TableNumber)
                    .ToListAsync();
                return View(model);
            }

            var conflict = await _context.Reservations
                .AnyAsync(r => r.TableId == model.TableId
                    && r.Status != "Cancelled"
                    && r.StartTime >= model.StartTime.AddMinutes(-30)
                    && r.StartTime <= model.StartTime.AddMinutes(30));

            if (conflict)
            {
                ModelState.AddModelError("StartTime", "Bàn đã được đặt trong khoảng thời gian này (±30 phút).");
                model.AvailableTables = await _context.BilliardTables
                    .Where(t => t.BranchId == branchId && t.StatusId == 1)
                    .OrderBy(t => t.TableNumber)
                    .ToListAsync();
                return View(model);
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == model.CustomerPhone);

            if (customer == null)
            {
                customer = new Customer
                {
                    Name = model.CustomerName,
                    Phone = model.CustomerPhone,
                    CreatedDate = DateTime.Now,
                    MembershipLevelId = 1,
                    UserId = "8ce77749-0568-4c8f-9115-d9facb09a689"
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            var reservation = new Reservation
            {
                CustomerId = customer.Id,
                TableId = model.TableId,
                BranchId = branchId.Value,
                StartTime = model.StartTime,
                Status = "Pending",
                CreatedDate = DateTime.Now,
                CreatedBy = User.Identity.Name
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new reservation with ID {ReservationId} for branch {BranchId}", reservation.Id, branchId);
            TempData["Success"] = "Đã tạo đơn đặt hàng thành công.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn đặt hàng." });
            }

            // Cập nhật trạng thái đơn và bàn
            reservation.Status = "Cancelled";
            reservation.UpdatedDate = DateTime.Now;
            reservation.UpdatedBy = User.Identity.Name;

            if (reservation.Table != null)
            {
                reservation.Table.StatusId = 1; // "Sẵn Sàng"
                _context.BilliardTables.Update(reservation.Table);
            }

            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cancelled reservation with ID {ReservationId} and set table {TableId} to available", id, reservation.TableId);
            return Json(new { success = true, message = "Đã hủy đơn đặt hàng thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> Confirm(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn đặt hàng." });
            }

            // Kiểm tra trạng thái bàn trước khi xác nhận
            if (reservation.Table == null || reservation.Table.StatusId != 1)
            {
                return Json(new { success = false, message = "Bàn không còn sẵn sàng để xác nhận." });
            }

            // Cập nhật trạng thái đơn và bàn
            reservation.Status = "Confirmed";
            reservation.UpdatedDate = DateTime.Now;
            reservation.UpdatedBy = User.Identity.Name;
            reservation.Table.StatusId = 3; // Giả định 3 là "Đã đặt"

            _context.Reservations.Update(reservation);
            _context.BilliardTables.Update(reservation.Table);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Confirmed reservation with ID {ReservationId} and set table {TableId} to reserved", id, reservation.TableId);
            return Json(new { success = true, message = "Đã xác nhận đơn đặt hàng thành công." });
        }
    }

    public class ReservationViewModel
    {
        public int TableId { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập tên khách hàng")]
        public string CustomerName { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string CustomerPhone { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn thời gian")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }
        public List<BilliardTable> AvailableTables { get; set; }
    }
}