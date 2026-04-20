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
        public IActionResult User()
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
        public IActionResult AddCollection()
        {
            return View();
        }
        public IActionResult RecordDonation()
        {
            return View();
        }
        public IActionResult ManageDonors()
        {
            return View();
        }
        public IActionResult TrackRequest()
        {
            return View();
        }
        public IActionResult ViewCollection()
        {
            return View();
        }
        public IActionResult ViewDonation()
        {
            return View();
        }
        public IActionResult RequestSuccess()
        {
            return View();
        }
        public IActionResult DonationSuccess()
        {
            return View();
        }
        public IActionResult ManageStaff()
        {
            return View();
        }
        public IActionResult ManageHospital()
        {
            return View();
        }
        public IActionResult ManageBloodStock()
        {
            return View();
        }
        public IActionResult CheckRequest()
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
