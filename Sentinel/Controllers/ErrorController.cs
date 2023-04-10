using Microsoft.AspNetCore.Mvc;

namespace Sentinel.Controllers
{
    public class ErrorController : Controller
    {
        public string Index()
        {
            return "1";
        }

        [Route("Error/{statusCode}")]
        public IActionResult ErrorHandler(int statusCode, string? message)
        {
            ViewData["ErrDetail"] = message;
            ViewData["ErrCode"] = statusCode;
            return View("Error");
        }
    }
}
