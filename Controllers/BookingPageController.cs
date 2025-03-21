using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiaProject.Data;

namespace WebBiaProject.Controllers
{
    public class BookingPageController : Controller
    {
       

        private readonly ILogger<BookingPageController> _logger;
        private readonly ApplicationDbContext _context;
        public BookingPageController(ApplicationDbContext context, ILogger<BookingPageController> logger)
        {
            _context = context;
            _logger = logger;
        }
        [HttpGet]
        public  IActionResult Index(int x)
        {
            var table = _context.BilliardTables
                .Include(b => b.Branch)
                .Where(b => b.Id == x).ToList();
            return View(table);
        }
    }

}
