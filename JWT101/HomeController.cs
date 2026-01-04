using Microsoft.AspNetCore.Mvc;

namespace JWT101
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
