using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebBiaProject.Data;
using WebBiaProject.Models;

namespace WebBiaProject.Areas.LeTan.Controllers
{
    [Area("Letan")]
    [Authorize(Roles = "Manager")]
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<InvoiceController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Lấy BranchId của người quản lý hiện tại
        private async Task<int?> GetManagerBranchId()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("Current user not found.");
                return null;
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
            {
                _logger.LogWarning("Employee record not found for user {UserId}.", user.Id);
                return null;
            }

            return employee.BranchId;
        }

        // GET: /ManagerBranch/Invoice/Index
        public async Task<IActionResult> Index()
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Could not determine the branch for the current manager.");
                TempData["Error"] = "Không thể xác định chi nhánh của bạn.";
                return RedirectToAction("Index", "Home");
            }

            _logger.LogInformation("Fetching invoices for branch {BranchId}.", branchId);
            var invoices = await _context.Invoices
                .Include(i => i.Customer) // Lấy thông tin khách hàng
                .Include(i => i.Branch)   // Lấy thông tin chi nhánh
                .Where(i => i.BranchId == branchId)
                .OrderByDescending(i => i.CreatedDate) // Sắp xếp theo ngày tạo mới nhất
                .ToListAsync();

            return View(invoices);
        }
    }
}