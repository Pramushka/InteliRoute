using Microsoft.AspNetCore.Mvc;

namespace InteliRoute.Controllers.Mvc
{
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
