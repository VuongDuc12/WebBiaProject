 using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

using NewAppBookShop.ViewModels;
using WebBiaProject.Data;
using WebBiaProject.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NewAppBookShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ApplicationDbContext _context;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
             _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var user = await userManager.FindByEmailAsync(model.Email);
                    if (user == null)
                    {
                        ModelState.AddModelError("", "Không tìm thấy người dùng với email này.");
                        return View(model);
                    }

                    // Check the user's role and redirect accordingly
                    if (await userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Index", "Home", new { area = "Admin" }); // Redirect to Admin Dashboard for Admin
                    }
                    else if (await userManager.IsInRoleAsync(user, "Manager"))
                    {
                        return RedirectToAction("Index", "Home", new { area = "ManagerBranch" }); // Redirect to ManagerBranch for Manager
                    }
                    else if (await userManager.IsInRoleAsync(user, "Employee"))
                    {
                        return RedirectToAction("Index", "Home", new { area = "Employee" }); // Redirect to Home page for Employee
                    }
                    else if (await userManager.IsInRoleAsync(user, "Cashier"))
                    {
                        return RedirectToAction("Index", "Home", new { area = "Cashier" }); // Redirect to Home page for Cashier
                    }
                    else if (await userManager.IsInRoleAsync(user, "Customer"))
                    {
                        return RedirectToAction("Index", "Home", new { area = "" }); // Redirect to Home page for Customer
                    }
                    else
                    {
                        ModelState.AddModelError("", "Bạn không có vai trò phù hợp để đăng nhập.");
                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                    return View(model);
                }
            }

            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
     public async Task<IActionResult> Register(RegisterViewModel model)
{
    if (ModelState.IsValid)
    {
                ApplicationUser user = new ApplicationUser
                {
                    Email = model.Email,
                    UserName = model.Email,
                    EmailConfirmed = true // Automatically confirm email
                };

        var result = await userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Assign "KhachHang" role
            await userManager.AddToRoleAsync(user, "Customer");
                    var khachHang = new Customer
                    {
                        UserId = user.Id,
                        Phone = model.SoDienThoai,
                        Email = model.Email,

                        Name = model.TenKh,
                        MembershipLevelId = 1 ,
                           TotalPaid = 0 // Bạn có thể mặc định hoặc lấy giá trị từ đâu đó
                    };

                // Thêm khách hàng vào DB
                _context.Customers.Add(khachHang);
                await _context.SaveChangesAsync();

                // Chuyển hướng đến trang khác sau khi đăng ký thành công
               return RedirectToAction("Login", "Account");

           
        }
        else
        {
            // Add any errors to the model state
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }
    }
    return View(model);
}


        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByNameAsync(model.Email);

                if(user == null)
                {
                    ModelState.AddModelError("", "Something is wrong!");
                    return View(model);
                }
                else
                {
                    return RedirectToAction("ChangePassword","Account", new {username = user.UserName});
                }
            }
            return View(model);
        }

      
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
