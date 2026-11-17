
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TsebeCareerPrep.Mvc.Controllers
{

    [Authorize(Roles = "Administrator,SuperAdministrator")]

    public class AdminController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
