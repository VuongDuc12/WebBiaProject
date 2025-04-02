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

            if (table.Status != null && table.Status.Name != "Sẵn Sàng")
            {
                _logger.LogWarning("Table {TableId} is not available for creating a new bill.", tableId);
                TempData["Error"] = $"Bàn {table.TableNumber} đang được sử dụng.";
                return RedirectToAction("Index");
            }

            var viewModel = new CreateBillViewModel
            {
                TableId = tableId,
                TableNumber = table.TableNumber,
                CustomerName = "Khach hang",
                CustomerPhone = "0"
            };

            return View(viewModel);
        }

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
                if (table == null)
                {
                    TempData["Error"] = "Không tìm thấy bàn với ID này.";
                    return RedirectToAction("Index");
                }
                model.TableNumber = table.TableNumber;
                return View(model);
            }

            var tableToUpdate = await _context.BilliardTables
                .Include(t => t.Status)
                .FirstOrDefaultAsync(t => t.BranchId == branchId && t.Id == model.TableId);

            if (tableToUpdate == null)
            {
                _logger.LogWarning("Billiard table with ID {TableId} not found.", model.TableId);
                TempData["Error"] = "Không tìm thấy bàn với ID này.";
                return RedirectToAction("Index");
            }

            if (tableToUpdate.Status.Name != "Sẵn Sàng")
            {
                _logger.LogWarning("Table {TableId} is not available.", model.TableId);
                TempData["Error"] = $"Bàn {tableToUpdate.TableNumber} đang được sử dụng.";
                return RedirectToAction("Index");
            }

            int? customerId = null;
            if (!string.IsNullOrEmpty(model.CustomerPhone))
            {
                var existingCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Phone == model.CustomerPhone);
                if (existingCustomer != null)
                {
                    customerId = existingCustomer.Id;
                }
                else
                {
                    var newCustomer = new Customer
                    {
                        UserId = "8ce77749-0568-4c8f-9115-d9facb09a689",
                        Name = model.CustomerName,
                        Phone = model.CustomerPhone,
                        CreatedDate = DateTime.Now,
                        MembershipLevelId = 1
                    };
                    _context.Customers.Add(newCustomer);
                    await _context.SaveChangesAsync();
                    customerId = newCustomer.Id;
                }
            }

            var newInvoice = new Invoice
            {
                BranchId = branchId.Value,
                TableId = model.TableId,
                CustomerId = customerId,
                CustomerName = model.CustomerName,
                CustomerPhone = model.CustomerPhone,
                CreatedDate = DateTime.Now,
                Status = "Đang xử lý",
                TotalAmount = 0,
                FinalAmount = 0,
                PaymentMethodId = 10
            };

            _context.Invoices.Add(newInvoice);
            await _context.SaveChangesAsync();

            var invoiceDetail = new InvoiceDetail
            {
                InvoiceId = newInvoice.Id,
                PlayTimeMinutes = 0,
                HourlyRate = tableToUpdate.HourlyRate,
                TotalPlayPrice = 0,
                TimeInput = DateTime.Now,
                TimeOutput = null,
                CreatedDate = DateTime.Now
            };

            _context.InvoiceDetails.Add(invoiceDetail);
            await _context.SaveChangesAsync();

            tableToUpdate.StatusId = 2; // "Đang chơi"
            _context.Update(tableToUpdate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new invoice with ID {InvoiceId} for table {TableId}.", newInvoice.Id, model.TableId);
            TempData["Success"] = $"Đã tạo hóa đơn mới cho bàn {tableToUpdate.TableNumber}.";

            return RedirectToAction("ViewBill", new { idTable = model.TableId });
        }

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
                if (table.Status.Name == "Sẵn Sàng")
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

        [HttpPost]
        public IActionResult AddProductToInvoice(int invoiceId, int productId, int quantity)
        {
            try
            {
                var invoice = _context.Invoices
                    .Include(i => i.ServiceProductDetails)
                    .FirstOrDefault(i => i.Id == invoiceId);
                if (invoice == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy hóa đơn." });
                }

                var product = _context.Products.FirstOrDefault(p => p.Id == productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                }

                var existingSpd = _context.ServiceProductDetails
                    .FirstOrDefault(spd => spd.ProductId == productId && spd.InvoiceId == invoiceId);

                int spdId;
                if (existingSpd != null)
                {
                    existingSpd.Quantity += quantity;
                    existingSpd.TotalPrice = existingSpd.Quantity * product.Price;
                    _context.Update(existingSpd);
                    spdId = existingSpd.Id;
                }
                else
                {
                    var spd = new ServiceProductDetail
                    {
                        InvoiceId = invoiceId,
                        ProductId = productId,
                        Quantity = quantity,
                        TotalPrice = quantity * product.Price
                    };
                    _context.ServiceProductDetails.Add(spd);
                    _context.SaveChanges();
                    spdId = spd.Id;
                }

                _context.SaveChanges();
                return Json(new { success = true, spdId = spdId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateProductQuantity(int spdId, int quantity)
        {
            try
            {
                var spd = _context.ServiceProductDetails
                    .Include(spd => spd.Product)
                    .FirstOrDefault(spd => spd.Id == spdId);
                if (spd == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                }

                spd.Quantity = quantity;
                spd.TotalPrice = spd.Quantity * spd.Product.Price;
                _context.Update(spd);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult RemoveProductFromInvoice(int spdId)
        {
            try
            {
                var spd = _context.ServiceProductDetails.Find(spdId);
                if (spd == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                }

                _context.ServiceProductDetails.Remove(spd);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult PayInvoice(int invoiceId, decimal productCost, decimal playCost)
        {
            try
            {
                var invoice = _context.Invoices
                    .Include(i => i.InvoiceDetails)
                    .FirstOrDefault(i => i.Id == invoiceId);
                if (invoice == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy hóa đơn." });
                }

                // Cập nhật thông tin hóa đơn
                invoice.TotalAmount = productCost;
                invoice.FinalAmount = productCost + playCost;
                invoice.Status = "Đã Thanh Toán";
               
                // Cập nhật trạng thái bàn
                var table = _context.BilliardTables
                    .FirstOrDefault(i => i.Id == invoice.TableId);
                if (table != null)
                {
                    table.StatusId = 1; // Giả sử 1 là trạng thái "Trống"
                   
                }
               
                _context.SaveChanges();
                _logger.LogInformation("cập nhật thông tin thành công");
                // Thiết lập thông báo TempData
                TempData["Success"] = $"Hóa đơn HD{invoice.Id} thanh toán thành công";

                // Chuyển hướng về trang Index
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        }

        public class CreateBillViewModel
    {
        public int TableId { get; set; }
        public int TableNumber { get; set; }

        [Display(Name = "Tên khách hàng")]
        public string CustomerName { get; set; }

        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string CustomerPhone { get; set; }
    }

    public class ViewBillViewModel
    {
        public Invoice Invoice { get; set; }
        public List<Product> Products { get; set; }
        public BilliardTable Table { get; set; }
    }
}