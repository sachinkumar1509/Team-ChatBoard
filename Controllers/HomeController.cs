using Microsoft.AspNetCore.Mvc;

namespace ChatBoard.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
