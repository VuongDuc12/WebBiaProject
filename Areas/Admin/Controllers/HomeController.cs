using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace WebBiaProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles ="Admin")]

    public class HomeController : Controller
    {
       

        public HomeController()
        {
            
        }

        public async Task<IActionResult> Index()
        {
            // Kết nối tới cơ sở dữ liệu
           
                // Lấy số lượng đơn hàng thành công
                var successfulOrdersCount = 100;
                
                // Lấy tổng doanh thu
                var totalRevenue = 1873641;
                
                // Đảm bảo giá trị không null
                ViewBag.SuccessfulOrdersCount = successfulOrdersCount;
                ViewBag.TotalRevenue = totalRevenue > 0 ? totalRevenue : 0;
         

            return View();
        }
    }
}
