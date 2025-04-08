using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebBiaProject.Data;
using WebBiaProject.Models;

namespace WebBiaProject.Areas.Reception.Controllers
{
    [Area("Reception")]
    [Authorize(Roles = "Reception")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<EmployeeController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
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

        // GET: /ManagerBranch/Employee/Index
        public async Task<IActionResult> Index()
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Could not determine the branch for the current manager.");
                TempData["Error"] = "Không thể xác định chi nhánh của bạn.";
                return RedirectToAction("Index", "Home");
            }

            _logger.LogInformation("Fetching employees for branch {BranchId}.", branchId);
            var employees = await _context.Employees
                .Include(e => e.Branch)
                .Include(e => e.Role)
                .Where(e => e.BranchId == branchId)
                .ToListAsync();

            return View(employees);
        }

        // GET: /ManagerBranch/Employee/Create
        public async Task<IActionResult> Create()
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Could not determine the branch for the current manager.");
                TempData["Error"] = "Không thể xác định chi nhánh của bạn.";
                return RedirectToAction("Index");
            }

            // Không cần ViewBag.Branches vì BranchId sẽ được gán tự động
            ViewBag.Roles = await _context.EmployeeRoles.ToListAsync();
            return View();
        }

        // POST: /ManagerBranch/Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Employee employee)
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Could not determine the branch for the current manager.");
                TempData["Error"] = "Không thể xác định chi nhánh của bạn.";
                return RedirectToAction("Index");
            }

            _logger.LogInformation("Received POST request for Create with data: Name={Name}, Email={Email}, Phone={Phone}, Address={Address}, Salary={Salary}, RoleId={RoleId}",
                employee.Name, employee.Email, employee.Phone, employee.Address, employee.Salary, employee.RoleId);

            // Loại bỏ lỗi ModelState không cần thiết
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("Role");
            ModelState.Remove("Branch"); // Loại bỏ lỗi cho Branch vì không gửi từ form
            ModelState.Remove("BranchId"); // Loại bỏ lỗi cho BranchId vì sẽ gán tự động

            // Gán BranchId tự động
            employee.BranchId = branchId.Value;

            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("ModelState Error for {Key}: {Error}", state.Key, error.ErrorMessage);
                    }
                }
                ViewBag.Roles = await _context.EmployeeRoles.ToListAsync();
                return View(employee);
            }

            // Kiểm tra email đã tồn tại
            var existingUser = await _userManager.FindByEmailAsync(employee.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Email {Email} already exists.", employee.Email);
                ModelState.AddModelError("", "Email đã được sử dụng, vui lòng chọn email khác.");
                ViewBag.Roles = await _context.EmployeeRoles.ToListAsync();
                return View(employee);
            }

            // Kiểm tra RoleId có tồn tại không
            var employeeRole = await _context.EmployeeRoles.FindAsync(employee.RoleId);
            if (employeeRole == null)
            {
                _logger.LogWarning("EmployeeRole with ID {RoleId} not found.", employee.RoleId);
                ModelState.AddModelError("", "Vai trò không tồn tại. Vui lòng chọn vai trò hợp lệ.");
                ViewBag.Roles = await _context.EmployeeRoles.ToListAsync();
                return View(employee);
            }

            // Ánh xạ EmployeeRole sang UserRole
            string userRoleName = employeeRole.Name switch
            {
                "Nhân Viên" => "Employee",
                "Quản Lý" => "Manager",
                "Thu Ngân" => "Cashier",
                _ => "Employee" // Mặc định nếu không khớp
            };

            // Kiểm tra và tạo UserRole nếu chưa tồn tại
            if (!await _roleManager.RoleExistsAsync(userRoleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(userRoleName));
                _logger.LogInformation("Created new UserRole: {RoleName}.", userRoleName);
            }

            // Tạo user trong AspNetUsers
            var user = new ApplicationUser
            {
                Email = employee.Email,
                UserName = employee.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, "nhanvien123@");
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} created successfully in AspNetUsers.", user.Email);
                await _userManager.AddToRoleAsync(user, userRoleName);

                // Gán thông tin cho employee
                employee.UserId = user.Id;
                employee.CreatedDate = DateTime.Now;
                employee.CreatedBy = User.Identity?.Name ?? "Manager";

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Employee {Name} created successfully with ID {Id}.", employee.Name, employee.Id);
                TempData["Success"] = "Nhân viên đã được tạo thành công!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("User creation failed: {Error}", error.Description);
                    ModelState.AddModelError("", error.Description);
                }
                ViewBag.Roles = await _context.EmployeeRoles.ToListAsync();
                return View(employee);
            }
        }

        // GET: /ManagerBranch/Employee/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Could not determine the branch for the current manager.");
                TempData["Error"] = "Không thể xác định chi nhánh của bạn.";
                return RedirectToAction("Index");
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                _logger.LogWarning("Employee with ID {Id} not found.", id);
                return NotFound();
            }

            // Kiểm tra xem nhân viên có thuộc chi nhánh của người quản lý không
            if (employee.BranchId != branchId)
            {
                _logger.LogWarning("Manager attempted to edit employee {EmployeeId} from unauthorized branch {BranchId}.", id, employee.BranchId);
                TempData["Error"] = "Bạn không có quyền chỉnh sửa nhân viên này.";
                return RedirectToAction("Index");
            }

            ViewBag.Branches = await _context.Branches.Where(b => b.Id == branchId).ToListAsync();
            ViewBag.Roles = await _context.EmployeeRoles.ToListAsync();
            return View(employee);
        }

        // POST: /ManagerBranch/Employee/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] Employee employee)
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Could not determine the branch for the current manager.");
                TempData["Error"] = "Không thể xác định chi nhánh của bạn.";
                return RedirectToAction("Index");
            }

            _logger.LogInformation("Received POST request for Edit with data: Id={Id}, Name={Name}, Email={Email}, Phone={Phone}, Address={Address}, Salary={Salary}, BranchId={BranchId}, RoleId={RoleId}",
                employee.Id, employee.Name, employee.Email, employee.Phone, employee.Address, employee.Salary, employee.BranchId, employee.RoleId);

            // Loại bỏ lỗi ModelState không cần thiết
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("Role");

            // Đảm bảo BranchId là chi nhánh của người quản lý
            if (employee.BranchId != branchId)
            {
                _logger.LogWarning("Manager attempted to edit employee for unauthorized branch {BranchId}.", employee.BranchId);
                ModelState.AddModelError("", "Bạn chỉ có thể chỉnh sửa nhân viên trong chi nhánh của mình.");
            }

            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("ModelState Error for {Key}: {Error}", state.Key, error.ErrorMessage);
                    }
                }
                ViewBag.Branches = await _context.Branches.Where(b => b.Id == branchId).ToListAsync();
                ViewBag.Roles = await _context.EmployeeRoles.ToListAsync();
                return View(employee);
            }

            var existingEmployee = await _context.Employees.FindAsync(employee.Id);
            if (existingEmployee == null)
            {
                _logger.LogWarning("Employee with ID {Id} not found during edit.", employee.Id);
                return NotFound();
            }

            // Kiểm tra xem nhân viên có thuộc chi nhánh của người quản lý không
            if (existingEmployee.BranchId != branchId)
            {
                _logger.LogWarning("Manager attempted to edit employee {EmployeeId} from unauthorized branch {BranchId}.", employee.Id, existingEmployee.BranchId);
                TempData["Error"] = "Bạn không có quyền chỉnh sửa nhân viên này.";
                return RedirectToAction("Index");
            }

            // Ánh xạ EmployeeRole sang UserRole
            var newEmployeeRole = await _context.EmployeeRoles.FindAsync(employee.RoleId);
            if (newEmployeeRole == null)
            {
                _logger.LogWarning("EmployeeRole with ID {RoleId} not found.", employee.RoleId);
                ModelState.AddModelError("", "Vai trò không tồn tại.");
                ViewBag.Branches = await _context.Branches.Where(b => b.Id == branchId).ToListAsync();
                ViewBag.Roles = await _context.EmployeeRoles.ToListAsync();
                return View(employee);
            }

            string newUserRoleName = newEmployeeRole.Name switch
            {
                "Nhân Viên" => "Employee",
                "Quản Lý" => "Manager",
                "Thu Ngân" => "Cashier",
                _ => "Employee"
            };

            // Cập nhật thông tin nhân viên
            existingEmployee.Name = employee.Name;
            existingEmployee.Email = employee.Email;
            existingEmployee.Phone = employee.Phone;
            existingEmployee.Address = employee.Address;
            existingEmployee.Salary = employee.Salary;
            existingEmployee.BranchId = employee.BranchId;
            existingEmployee.RoleId = employee.RoleId;
            existingEmployee.UpdatedDate = DateTime.Now;
            existingEmployee.UpdatedBy = User.Identity?.Name ?? "Manager";

            // Cập nhật email và vai trò trong AspNetUsers
            var user = await _userManager.FindByIdAsync(existingEmployee.UserId);
            if (user != null)
            {
                if (user.Email != employee.Email)
                {
                    _logger.LogInformation("Updating email for user {UserId} from {OldEmail} to {NewEmail}.", user.Id, user.Email, employee.Email);
                    user.Email = employee.Email;
                    user.UserName = employee.Email;
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        foreach (var error in updateResult.Errors)
                        {
                            _logger.LogWarning("Failed to update user email: {Error}", error.Description);
                            ModelState.AddModelError("", error.Description);
                        }
                        ViewBag.Branches = await _context.Branches.Where(b => b.Id == branchId).ToListAsync();
                        ViewBag.Roles = await _context.EmployeeRoles.ToListAsync();
                        return View(employee);
                    }
                }

                // Cập nhật vai trò trong AspNetUsers
                var currentRoles = await _userManager.GetRolesAsync(user);
                var currentUserRole = currentRoles.FirstOrDefault();
                if (currentUserRole != newUserRoleName)
                {
                    if (currentUserRole != null)
                    {
                        await _userManager.RemoveFromRoleAsync(user, currentUserRole);
                        _logger.LogInformation("Removed user {UserId} from role {Role}.", user.Id, currentUserRole);
                    }
                    await _userManager.AddToRoleAsync(user, newUserRoleName);
                    _logger.LogInformation("Added user {UserId} to role {Role}.", user.Id, newUserRoleName);
                }
            }

            _context.Update(existingEmployee);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Employee {Name} with ID {Id} updated successfully.", existingEmployee.Name, existingEmployee.Id);
            TempData["Success"] = "Nhân viên đã được cập nhật thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /ManagerBranch/Employee/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Delete(int id)
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Could not determine the branch for the current manager.");
                return Json(new { success = false, message = "Không thể xác định chi nhánh của bạn." });
            }

            _logger.LogInformation("Received DELETE request for Employee with ID {Id}.", id);

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                _logger.LogWarning("Employee with ID {Id} not found for deletion.", id);
                return Json(new { success = false, message = "Không tìm thấy nhân viên." });
            }

            // Kiểm tra xem nhân viên có thuộc chi nhánh của người quản lý không
            if (employee.BranchId != branchId)
            {
                _logger.LogWarning("Manager attempted to delete employee {EmployeeId} from unauthorized branch {BranchId}.", id, employee.BranchId);
                return Json(new { success = false, message = "Bạn không có quyền xóa nhân viên này." });
            }

            // Xóa user trong AspNetUsers
            var user = await _userManager.FindByIdAsync(employee.UserId);
            if (user != null)
            {
                var deleteUserResult = await _userManager.DeleteAsync(user);
                if (!deleteUserResult.Succeeded)
                {
                    foreach (var error in deleteUserResult.Errors)
                    {
                        _logger.LogWarning("Failed to delete user {UserId}: {Error}", user.Id, error.Description);
                    }
                    return Json(new { success = false, message = "Không thể xóa tài khoản người dùng." });
                }
                _logger.LogInformation("User {UserId} deleted successfully.", user.Id);
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Employee {Name} with ID {Id} deleted successfully.", employee.Name, employee.Id);
            return Json(new { success = true, message = "Nhân viên đã được xóa thành công." });
        }
    }
}