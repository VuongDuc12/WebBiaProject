using Microsoft.AspNetCore.Mvc;

namespace WebBiaProject.Controllers
{
    public class BookingPageController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }

}
