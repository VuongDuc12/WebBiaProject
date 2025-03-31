using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiaProject.Data;
using WebBiaProject.Models;

namespace WebBiaProject.Areas.ManagerBranch.Controllers
{
    [Area("ManagerBranch")]
    [Authorize(Roles = "Manager")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Lấy thông tin Employee của quản lý hiện tại, bao gồm Branch
        private async Task<Employee?> GetCurrentManager()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return null;
            }

            var employee = await _context.Employees
                .Include(e => e.Branch) // Include Branch để lấy thông tin chi nhánh
                .FirstOrDefaultAsync(e => e.UserId == user.Id && e.Role.Name == "Quản Lý");

            return employee;
        }

        public async Task<IActionResult> Index()
        {
            var manager = await GetCurrentManager();
            if (manager == null)
            {
                TempData["Error"] = "Không thể xác định thông tin quản lý của bạn.";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Truyền thông tin quản lý và chi nhánh vào ViewBag
            ViewBag.ManagerName = manager.Name;
            ViewBag.BranchName = manager.Branch?.Name ?? "Không xác định";
            ViewBag.BranchAddress = manager.Branch?.Address ?? "Không xác định";

            return View();
        }
    }
}