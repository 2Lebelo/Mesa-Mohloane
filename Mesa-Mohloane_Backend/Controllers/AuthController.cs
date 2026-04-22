using Microsoft.AspNetCore.Mvc;

namespace Mesa_Mohloane_Backend.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
