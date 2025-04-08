using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

        // GET: /Admin/Employee/Index
        public IActionResult Index()
        {
            _logger.LogInformation("Fetching all employees with related Branch and Role data.");
            var employees = _context.Employees
                .Include(e => e.Branch)
                .Include(e => e.Role)
                .ToList();
            return View(employees);
        }

        // GET: /Admin/Employee/Create
        public IActionResult Create()
        {
            var branches = _context.Branches.ToList();
            var roles = _context.EmployeeRoles.ToList();

            if (!branches.Any())
            {
                _logger.LogWarning("No branches found in the database.");
                TempData["Error"] = "Không có chi nhánh nào trong database. Vui lòng tạo chi nhánh trước.";
            }

            if (!roles.Any())
            {
                _logger.LogWarning("No roles found in the database.");
                TempData["Error"] = "Không có vai trò nào trong database. Vui lòng tạo vai trò trước.";
            }

            ViewBag.Branches = branches;
            ViewBag.Roles = roles;
            return View();
        }

        // POST: /Admin/Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Employee employee)
        {
            _logger.LogInformation("Received POST request for Create with data: Name={Name}, Email={Email}, Phone={Phone}, Address={Address}, Salary={Salary}, BranchId={BranchId}, RoleId={RoleId}",
                employee.Name, employee.Email, employee.Phone, employee.Address, employee.Salary, employee.BranchId, employee.RoleId);

            // Loại bỏ lỗi ModelState cho các trường không cần thiết
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("Role");

            // Kiểm tra ModelState
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("ModelState Error for {Key}: {Error}", state.Key, error.ErrorMessage);
                    }
                }
                ViewBag.Branches = _context.Branches.ToList();
                ViewBag.Roles = _context.EmployeeRoles.ToList();
                return View(employee);
            }

            // Kiểm tra email đã tồn tại
            var existingUser = await _userManager.FindByEmailAsync(employee.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Email {Email} already exists.", employee.Email);
                ModelState.AddModelError("", "Email đã được sử dụng, vui lòng chọn email khác.");
                ViewBag.Branches = _context.Branches.ToList();
                ViewBag.Roles = _context.EmployeeRoles.ToList();
                return View(employee);
            }

            // Kiểm tra RoleId có tồn tại không
            var employeeRole = await _context.EmployeeRoles.FindAsync(employee.RoleId);
            if (employeeRole == null)
            {
                _logger.LogWarning("EmployeeRole with ID {RoleId} not found.", employee.RoleId);
                ModelState.AddModelError("", "Vai trò không tồn tại. Vui lòng chọn vai trò hợp lệ.");
                ViewBag.Branches = _context.Branches.ToList();
                ViewBag.Roles = _context.EmployeeRoles.ToList();
                return View(employee);
            }

            // Ánh xạ EmployeeRole sang UserRole
            string userRoleName = employeeRole.Name switch
            {
                "Nhân Viên" => "Employee",
                "Quản Lý" => "Manager",
                "Lễ Tân" => "Reception",
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

            var result = await _userManager.CreateAsync(user, "Nhanvien123@");
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} created successfully in AspNetUsers.", user.Email);
                await _userManager.AddToRoleAsync(user, userRoleName);

                // Gán thông tin cho employee
                employee.UserId = user.Id;
                employee.CreatedDate = DateTime.Now;
                employee.CreatedBy = User.Identity?.Name ?? "Admin";

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
                ViewBag.Branches = _context.Branches.ToList();
                ViewBag.Roles = _context.EmployeeRoles.ToList();
                return View(employee);
            }
        }


        // GET: /Admin/Employee/Edit/{id}
        public IActionResult Edit(int id)
        {
            var employee = _context.Employees.Find(id);
            if (employee == null)
            {
                _logger.LogWarning("Employee with ID {Id} not found.", id);
                return NotFound();
            }

            ViewBag.Branches = _context.Branches.ToList();
            ViewBag.Roles = _context.EmployeeRoles.ToList();
            return View(employee);
        }

        // POST: /Admin/Employee/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] Employee employee)
        {
            _logger.LogInformation("Received POST request for Edit with data: Id={Id}, Name={Name}, Email={Email}, Phone={Phone}, Address={Address}, Salary={Salary}, BranchId={BranchId}, RoleId={RoleId}",
                employee.Id, employee.Name, employee.Email, employee.Phone, employee.Address, employee.Salary, employee.BranchId, employee.RoleId);

            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("ModelState Error for {Key}: {Error}", state.Key, error.ErrorMessage);
                    }
                }
                ViewBag.Branches = _context.Branches.ToList();
                ViewBag.Roles = _context.EmployeeRoles.ToList();
                return View(employee);
            }

            var existingEmployee = await _context.Employees.FindAsync(employee.Id);
            if (existingEmployee == null)
            {
                _logger.LogWarning("Employee with ID {Id} not found during edit.", employee.Id);
                return NotFound();
            }

            // Ánh xạ EmployeeRole sang UserRole
            var newEmployeeRole = await _context.EmployeeRoles.FindAsync(employee.RoleId);
            if (newEmployeeRole == null)
            {
                _logger.LogWarning("EmployeeRole with ID {RoleId} not found.", employee.RoleId);
                ModelState.AddModelError("", "Vai trò không tồn tại.");
                ViewBag.Branches = _context.Branches.ToList();
                ViewBag.Roles = _context.EmployeeRoles.ToList();
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
            existingEmployee.UpdatedBy = User.Identity?.Name ?? "Admin";

            // Cập nhật email và vai trò trong AspNetUsers
            var user = await _userManager.FindByIdAsync(existingEmployee.UserId);
            if (user != null)
            {
                // Cập nhật email nếu thay đổi
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
                        ViewBag.Branches = _context.Branches.ToList();
                        ViewBag.Roles = _context.EmployeeRoles.ToList();
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

        // POST: /Admin/Employee/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Delete(int id)
        {
            _logger.LogInformation("Received DELETE request for Employee with ID {Id}.", id);

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                _logger.LogWarning("Employee with ID {Id} not found for deletion.", id);
                return Json(new { success = false, message = "Không tìm thấy nhân viên." });
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