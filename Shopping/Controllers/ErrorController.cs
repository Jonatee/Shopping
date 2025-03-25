using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Shopping.Controllers
{
    public class ErrorController : Controller
    {
        // GET: ErrorController
        public IActionResult GeneralError()
        {
            return View();
        }

        public IActionResult NotFound()
        {
            return View();
        }

        public IActionResult InternalServerError()
        {
            return View();
        }
       


    }
}
