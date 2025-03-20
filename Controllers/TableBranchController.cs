using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiaProject.Data;
using WebBiaProject.Models;
using WebBiaProject.ViewModel;

namespace WebBiaProject.Controllers
{
    public class TableBranchController : Controller
    {
        private readonly ILogger<TableBranchController> _logger;
        private readonly ApplicationDbContext _context;
        public TableBranchController(ApplicationDbContext context, ILogger<TableBranchController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<IActionResult> Index(int x)
        {
            var branch = await _context.Branches.FindAsync(x);
            var result = _context.BilliardTables
                .Include(b => b.Status)
                .Where(b => b.BranchId == x).ToList();

            if (branch == null)
            {
                return NotFound(); // Trả về lỗi 404 nếu không tìm thấy chi nhánh
            }

            var tableBranchView = new TableBranchViewModel
            {
                Branch = branch,
                billiardTables = result
            };

            return View(tableBranchView);
        }
        public IActionResult DatNgay(int tableId, int branchId)
        {
            
            return RedirectToAction("BookTable", "TableBranch", new { tableId, branchId });
        }
    }
}
