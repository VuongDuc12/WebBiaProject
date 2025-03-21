using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using WebBiaProject.Models;


namespace WebBiaProject.Areas.Admin.Controllers
{

    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        // Constructor to inject services
        public UsersController(UserManager<ApplicationUser> userMgr, RoleManager<IdentityRole> roleMgr)
        {
            userManager = userMgr;
            roleManager = roleMgr;
        }

        // Action to list users with their roles
        public async Task<IActionResult> Index()
        {
            var users = userManager.Users.ToList();
            var userRoleData = new List<UsersRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                userRoleData.Add(new UsersRoleViewModel
                {
                    UserId = user.Id,
                    UserEmail = user.Email,
                    Roles = string.Join(", ", roles) // Join roles as comma separated string
                });
            }

            return View(userRoleData); // Passing role data to the view
        }

        // Other actions like Create, Edit, Delete...
    
     // Action để chỉnh sửa roles của người dùng
       public async Task<IActionResult> Edit(string userId)
{
    var user = await userManager.FindByIdAsync(userId);
    if (user == null) return NotFound();

    // Get user's current roles
    var userRoles = await userManager.GetRolesAsync(user);

    // Get all available roles
    var allRoles = roleManager.Roles.Select(r => r.Name).ToList();

    // Initialize ViewModel with user data and roles
    var model = new EditRolesViewModel
    {
        UserId = userId,
        UserEmail = user.Email,
        AllRoles = allRoles,
        UserRoles = userRoles.ToList(),
        SelectedRoles = userRoles.ToList() // Initialize with the current user's roles
    };

    return View(model);
}

        // Action xử lý khi cập nhật roles của người dùng
        [HttpPost]
        public async Task<IActionResult> Edit(EditRolesViewModel model)
        {
            var user = await userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            // Lấy các role hiện tại của người dùng
            var currentRoles = await userManager.GetRolesAsync(user);

            // Thêm các role mới cho người dùng
            var addRoles = model.SelectedRoles.Except(currentRoles);
            foreach (var role in addRoles)
            {
                await userManager.AddToRoleAsync(user, role);
            }

            // Xóa các role không còn thuộc về người dùng nữa
            var removeRoles = currentRoles.Except(model.SelectedRoles);
            foreach (var role in removeRoles)
            {
                await userManager.RemoveFromRoleAsync(user, role);
            }

            return RedirectToAction("Index"); // Chuyển hướng sau khi cập nhật
        }
    

    // ViewModel cho việc cập nhật roles của người dùng
    public class EditRolesViewModel
    {
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public List<string> AllRoles { get; set; }
        public List<string> UserRoles { get; set; }
        public List<string> SelectedRoles { get; set; }

 
}




    // ViewModel for passing user and roles to the view
    public class UsersRoleViewModel
    {
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public string Roles { get; set; }
    }

}
}
