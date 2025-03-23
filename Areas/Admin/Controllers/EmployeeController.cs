using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiaProject.Data;
using WebBiaProject.Models;

namespace WebBiaProject.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var employees = _context.Employees
                .Include(e => e.Branch)
                .Include(e => e.Role)
                .ToList();
            return View(employees);
        }

        public IActionResult Create()
        {
            var branches = _context.Branches.ToList();
            if (!branches.Any())
            {
                Console.WriteLine("Không có chi nhánh nào trong database!");
            }
            ViewBag.Branches = branches;
            ViewBag.Roles = _context.EmployeeRoles.ToList();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Branches = _context.Branches.ToList();
                ViewBag.Roles = _context.EmployeeRoles.ToList();
                return View(employee);
            }

            try
            {
                // Kiểm tra RoleId hợp lệ
                if (!_context.EmployeeRoles.Any(r => r.Id == employee.RoleId))
                {
                    ModelState.AddModelError("", "RoleId không hợp lệ.");
                    return View(employee);
                }

                // Kiểm tra BranchId hợp lệ
                if (employee.BranchId != null && !_context.Branches.Any(b => b.Id == employee.BranchId))
                {
                    ModelState.AddModelError("", "BranchId không hợp lệ.");
                    return View(employee);
                }

                // Tạo user trong Identity
                var user = new ApplicationUser
                {
                    Email = employee.Email,
                    UserName = employee.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, "Test123!");
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Lỗi Identity: {error.Code} - {error.Description}");
                        ModelState.AddModelError("", error.Description);
                    }

                    ViewBag.Branches = _context.Branches.ToList();
                    ViewBag.Roles = _context.EmployeeRoles.ToList();
                    return View(employee);
                }

                // Thêm vào Role "Employee"
                await _userManager.AddToRoleAsync(user, "Employee");

                // Gán thông tin UserId
                employee.UserId = user.Id;
                employee.CreatedDate = DateTime.Now;
                employee.CreatedBy = User.Identity?.Name ?? "System";

                // Thêm Employee vào database
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Employee");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi tạo nhân viên: " + ex.Message);
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                ViewBag.Branches = _context.Branches.ToList();
                ViewBag.Roles = _context.EmployeeRoles.ToList();
                return View(employee);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Employee employee)
        {
            if (ModelState.IsValid)
            {
                var existingEmployee = _context.Employees.Find(employee.Id);
                if (existingEmployee == null) return NotFound();

                // Cập nhật thông tin nhân viên
                existingEmployee.Name = employee.Name;
                existingEmployee.Email = employee.Email;
                existingEmployee.Phone = employee.Phone;
                existingEmployee.Address = employee.Address;
                existingEmployee.Salary = employee.Salary;
                existingEmployee.BranchId = employee.BranchId;
                existingEmployee.RoleId = employee.RoleId;
                existingEmployee.UpdatedDate = DateTime.Now;
                existingEmployee.UpdatedBy = User.Identity.Name;

                // Cập nhật email trong AspNetUsers nếu thay đổi
                var user = await _userManager.FindByIdAsync(existingEmployee.UserId);
                if (user != null && user.Email != employee.Email)
                {
                    user.Email = employee.Email;
                    user.UserName = employee.Email; // Cập nhật username nếu cần
                    await _userManager.UpdateAsync(user);
                }

                _context.Update(existingEmployee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Branches = _context.Branches.ToList();
            ViewBag.Roles = _context.EmployeeRoles.ToList();
            return View(employee);
        }

        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            var employee = _context.Employees.Find(id);
            if (employee == null) return Json(new { success = false, message = "Không tìm thấy nhân viên" });

            // Xóa user trong AspNetUsers
            var user = await _userManager.FindByIdAsync(employee.UserId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}