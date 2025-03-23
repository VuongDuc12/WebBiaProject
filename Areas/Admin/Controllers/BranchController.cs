using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiaProject.Data;
using WebBiaProject.Models;

namespace WebBiaProject.Controllers
{

    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BranchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BranchController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Index
        public IActionResult Index()
        {
            var branches = _context.Branches.ToList();
            return View(branches);
        }

        // GET: Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Branch branch)
        {
            if (ModelState.IsValid)
            {
                branch.CreatedDate = DateTime.Now;
                branch.CreatedBy = User.Identity.Name; // Hoặc lấy từ hệ thống đăng nhập
                _context.Add(branch);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(branch);
        }

        // GET: Edit
        public IActionResult Edit(int id)
        {
            var branch = _context.Branches.Find(id);
            if (branch == null)
            {
                return NotFound();
            }
            return View(branch);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Branch branch)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingBranch = _context.Branches.Find(branch.Id);
                    if (existingBranch == null)
                    {
                        return NotFound();
                    }

                    existingBranch.Name = branch.Name;
                    existingBranch.Address = branch.Address;
                    existingBranch.Phone = branch.Phone;
                    existingBranch.Status = branch.Status;
                    existingBranch.UpdatedDate = DateTime.Now;
                    existingBranch.UpdatedBy = User.Identity.Name;

                    _context.Update(existingBranch);
                    _context.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Branches.Any(e => e.Id == branch.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            return View(branch);
        }

        // POST: Delete
        [HttpPost]
        public JsonResult Delete(int id)
        {
            var branch = _context.Branches.Find(id);
            if (branch == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chi nhánh" });
            }

            _context.Branches.Remove(branch);
            _context.SaveChanges();
            return Json(new { success = true });
        }
    }
}