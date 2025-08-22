using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteliRoute.Controllers.Mvc
{
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Dashboard() 
        {
            return View();
        }
    }
}
