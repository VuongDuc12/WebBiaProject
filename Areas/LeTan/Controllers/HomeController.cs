using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace WebBiaProject.Areas.LeTan.Controllers
{
    [Area("Letan")]
    [Authorize(Roles ="Manager")]

    public class HomeController : Controller
    {
       

        public HomeController()
        {
            
        }

        public async Task<IActionResult> Index()
        {
            

            return View();
        }
    }
}
