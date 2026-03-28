using System.Diagnostics;
using BBMS.Models;
using Microsoft.AspNetCore.Mvc;

namespace BBMS.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult login()
        {
            return View();
        }
        public IActionResult Donor()
        {
            return View();
        }
        public IActionResult Request()
        {
            return View();
        }
        public IActionResult Staff ()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Admin()
        {
            return View();
        }
        public IActionResult Donate()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
