using Microsoft.AspNetCore.Mvc;
using WebBiaProject.Models;
using WebBiaProject.ViewModel;

namespace WebBiaProject.Controllers
{
    public class TableBranchController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult DatNgay(int tableId, int branchId)
        {
            
            return RedirectToAction("BookTable", "TableBranch", new { tableId, branchId });
        }
    }
}
