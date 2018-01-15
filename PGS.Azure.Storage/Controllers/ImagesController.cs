using Microsoft.AspNetCore.Mvc;

namespace PGS.Azure.Storage.Controllers
{
    public class ImagesController : Controller
    {
        // GET
        public IActionResult Index()
        {
            return View();
        }
    }
}