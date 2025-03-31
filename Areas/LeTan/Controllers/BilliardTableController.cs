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
    public class BilliardTableController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<BilliardTableController> _logger;

        public BilliardTableController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<BilliardTableController> logger)
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

        // GET: /ManagerBranch/BilliardTable/Index
        public async Task<IActionResult> Index()
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Could not determine the branch for the current manager.");
                TempData["Error"] = "Không thể xác định chi nhánh của bạn.";
                return RedirectToAction("Index", "Home");
            }

            _logger.LogInformation("Fetching billiard tables for branch {BranchId}.", branchId);
            var tables = await _context.BilliardTables
                .Where(t => t.BranchId == branchId)
                .OrderBy(t => t.TableNumber) // Sắp xếp theo số bàn
                .ToListAsync();

            return View(tables);
        }
    }
}