using BBMS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YourProjectName.Models;

namespace YourProjectName.Controllers
{
    public class ReceiverController : Controller
    {
        private readonly AppDbContext _context;

        public ReceiverController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Receiver/MyRequests
        // Shows all requests submitted by the logged-in receiver
        public async Task<IActionResult> MyRequests()
        {
            var requests = await _context.RequestBloods
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // GET: Receiver/Details/5
        // Shows full details of a single request
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.RequestBloods
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            return View(request);
        }

        // GET: Receiver/RequestBlood
        // Show the blood request form
        public IActionResult RequestBlood()
        {
            return View();
        }

        // POST: Receiver/RequestBlood
        // Save new blood request to database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestBlood(RequestBlood model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.Status = RequestStatus.Pending;
                model.PaymentStatus = PaymentStatus.Unpaid;
                model.TotalExpenseAmount = 165;

                _context.RequestBloods.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction("RequestSuccess");
            }

            return View(model);
        }

        // GET: Receiver/RequestSuccess
        // Confirmation page after successful submission
        public IActionResult RequestSuccess()
        {
            return View();
        }

        // POST: Receiver/Cancel/5
        // Receiver cancels their own pending request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var request = await _context.RequestBloods
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            if (request.Status != RequestStatus.Pending)
            {
                TempData["Error"] = "Only pending requests can be cancelled.";
                return RedirectToAction("MyRequests");
            }

            request.Status = RequestStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your request has been cancelled.";
            return RedirectToAction("MyRequests");
        }
    }
}