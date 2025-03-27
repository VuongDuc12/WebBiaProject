using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebBiaProject.Areas.Admin.ViewModel;
using WebBiaProject.Data;
using WebBiaProject.Models;
using System.Linq;
using System.Threading.Tasks;

namespace WebBiaProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BranchController : Controller
    {
        private readonly ILogger<BranchController> _logger;
        private readonly ApplicationDbContext _context;

        public BranchController(ILogger<BranchController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: /Admin/Branch/Index
        [HttpGet]
        public IActionResult Index(string search)
        {
            var branches = _context.Branches.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                branches = branches.Where(b => b.Name.Contains(search) || b.Address.Contains(search));
                ViewBag.SearchQuery = search;
            }

            _logger.LogInformation("Accessed Index with search: {Search}", search);
            return View(branches.ToList());
        }

        // GET: /Admin/Branch/Create
        [HttpGet]
        public IActionResult Create()
        {
            _logger.LogInformation("Accessed Create GET action");
            return View(new BranchViewModel());
        }

        // POST: /Admin/Branch/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] BranchViewModel model)
        {
            _logger.LogInformation("Received POST request for Create with model data: Name={Name}, Address={Address}, Phone={Phone}, Status={Status}",
                model.Name ?? "(null)", model.Address ?? "(null)", model.Phone ?? "(null)", model.Status ?? "(null)");

            _logger.LogInformation("Raw Form Data: Name={Name}, Address={Address}, Phone={Phone}, Status={Status}",
                Request.Form["Name"], Request.Form["Address"], Request.Form["Phone"], Request.Form["Status"]);

            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("ModelState Error for {Key}: {Error}", state.Key, error.ErrorMessage);
                    }
                }
                return View(model);
            }

            var branch = new Branch
            {
                Name = model.Name,
                Address = model.Address,
                Phone = model.Phone,
                Status = model.Status,
                CreatedDate = DateTime.Now
            };

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Branch created successfully: {Name}", model.Name);
            TempData["Success"] = "Branch created successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}