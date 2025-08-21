using Microsoft.AspNetCore.Mvc;

namespace InteliRoute.Controllers.Mvc
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
