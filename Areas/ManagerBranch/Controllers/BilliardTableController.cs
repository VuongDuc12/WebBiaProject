using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using WebBiaProject.Data;
using WebBiaProject.Models;

namespace WebBiaProject.Areas.ManagerBranch.Controllers
{
    [Area("ManagerBranch")]
    [Authorize(Roles = "Manager")]
    public class BilliardTableController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<BilliardTableController> _logger;

        public BilliardTableController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<BilliardTableController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

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

        public async Task<IActionResult> Index()
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Could not determine the branch for the current manager.");
                TempData["Error"] = "Không thể xác định chi nhánh của bạn.";
                return RedirectToAction("Index", "Home");
            }

            _logger.LogInformation("Fetching billiard tables for branch {BranchId}.", branchId);
            var tables = await _context.BilliardTables
                .Include(t => t.Status)
                .Where(t => t.BranchId == branchId)
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            return View(tables);
        }

        // GET: Hiển thị form tạo hóa đơn và nhập thông tin khách hàng
        [HttpGet]
        public async Task<IActionResult> CreateBill(int tableId)
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Could not determine the branch for the current manager.");
                TempData["Error"] = "Không thể xác định chi nhánh của bạn.";
                return RedirectToAction("Index", "Home");
            }

            var table = await _context.BilliardTables
                .Include(t => t.Status)
                .FirstOrDefaultAsync(t => t.BranchId == branchId && t.Id == tableId);

            if (table == null)
            {
                _logger.LogWarning("Billiard table with ID {TableId} not found.", tableId);
                TempData["Error"] = "Không tìm thấy bàn với ID này.";
                return RedirectToAction("Index");
            }

            if (table.Status != null && table.Status.Name != "Trống")
            {
                _logger.LogWarning("Table {TableId} is not available for creating a new bill.", tableId);
                TempData["Error"] = $"Bàn {table.TableNumber} đang được sử dụng.";
                return RedirectToAction("Index");
            }

            var viewModel = new CreateBillViewModel
            {
                TableId = tableId,
                TableNumber = table.TableNumber
            };

            return View(viewModel);
        }

        // POST: Xử lý tạo hóa đơn với thông tin khách hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBill(CreateBillViewModel model)
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Could not determine the branch for the current manager.");
                TempData["Error"] = "Không thể xác định chi nhánh của bạn.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                var table = await _context.BilliardTables
                    .FirstOrDefaultAsync(t => t.Id == model.TableId);
                model.TableNumber = table?.TableNumber;
                return View(model);
            }

            var tableToUpdate = await _context.BilliardTables
                .Include(t => t.Status)
                .FirstOrDefaultAsync(t => t.BranchId == branchId && t.Id == model.TableId);

            if (tableToUpdate == null || tableToUpdate.Status.Name != "Trống")
            {
                _logger.LogWarning("Table {TableId} is not available.", model.TableId);
                TempData["Error"] = "Bàn không còn trống hoặc không tồn tại.";
                return RedirectToAction("Index");
            }

            // Tạo khách hàng mới nếu có thông tin
            Customer customer = null;
            if (!string.IsNullOrEmpty(model.CustomerName) || !string.IsNullOrEmpty(model.CustomerPhone))
            {
                customer = new Customer
                {
                    Name = model.CustomerName,
                    Phone = model.CustomerPhone
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            // Tạo hóa đơn mới
            var newInvoice = new Invoice
            {
                BranchId = branchId.Value,
                TableId = model.TableId,
                CustomerId = customer?.Id,
                CreatedDate = DateTime.Now,
                Status = "Đang xử lý"
            };

            _context.Invoices.Add(newInvoice);
            await _context.SaveChangesAsync();

            // Cập nhật trạng thái bàn
            tableToUpdate.StatusId = 2; // Giả sử StatusId 2 là "Đang sử dụng"
            _context.Update(tableToUpdate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new invoice with ID {InvoiceId} for table {TableId}.", newInvoice.Id, model.TableId);
            TempData["Success"] = $"Đã tạo hóa đơn mới cho bàn {tableToUpdate.TableNumber}.";

            return RedirectToAction("ViewBill", new { idTable = model.TableId });
        }

        // Action hiển thị hoặc tạo hóa đơn theo idTable
        public async Task<IActionResult> ViewBill(int idTable)
        {
            var branchId = await GetManagerBranchId();
            if (branchId == null)
            {
                _logger.LogError("Could not determine the branch for the current manager.");
                TempData["Error"] = "Không thể xác định chi nhánh của bạn.";
                return RedirectToAction("Index", "Home");
            }

            var table = await _context.BilliardTables
                .Include(t => t.Status)
                .FirstOrDefaultAsync(t => t.BranchId == branchId && t.Id == idTable);

            if (table == null)
            {
                _logger.LogWarning("Billiard table with ID {IdTable} not found.", idTable);
                TempData["Error"] = "Không tìm thấy bàn với ID này.";
                return RedirectToAction("Index");
            }

            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceDetails)
                .Include(i => i.ServiceProductDetails)
                    .ThenInclude(spd => spd.Product)
                .FirstOrDefaultAsync(i => i.TableId == idTable && i.BranchId == branchId && i.Status == "Đang xử lý");

            if (invoice == null)
            {
                if (table.Status.Name == "Trống")
                {
                    return RedirectToAction("CreateBill", new { tableId = idTable });
                }
                else
                {
                    _logger.LogWarning("No active invoice found for table {IdTable}, and table is not available.", idTable);
                    TempData["Error"] = $"Bàn {table.TableNumber} không có hóa đơn đang xử lý.";
                    return RedirectToAction("Index");
                }
            }

            var products = await _context.Products
                .OrderBy(p => p.Name)
                .ToListAsync();

            var viewModel = new ViewBillViewModel
            {
                Invoice = invoice,
                Products = products,
                Table = table
            };

            return View(viewModel);
        }
    }

    // ViewModel cho CreateBill
    public class CreateBillViewModel
    {
        public int TableId { get; set; }
        public string TableNumber { get; set; }

        [Display(Name = "Tên khách hàng")]
        public string CustomerName { get; set; }

        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string CustomerPhone { get; set; }
    }

    // ViewModel cho ViewBill
    public class ViewBillViewModel
    {
        public Invoice Invoice { get; set; }
        public List<Product> Products { get; set; }
        public BilliardTable Table { get; set; }
    }
}